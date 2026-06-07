using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using backend.Data;
using backend.Dtos;
using backend.models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class GoogleCalendarService
{
    private readonly IConfiguration _config;
    private readonly Database _db;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GoogleCalendarService(IConfiguration config, Database db)
    {
        _config = config;
        _db = db;
        _clientId = config["Google:ClientId"] ?? "";
        _clientSecret = config["Google:ClientSecret"] ?? "";
        _redirectUri = config["Google:RedirectUri"] ?? "http://localhost:5000/api/calendar/google/callback";
    }

    public string BuildAuthUrl(string stateToken)
    {
        var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/calendar.readonly");
        var redirectUri = Uri.EscapeDataString(_redirectUri);
        var state = Uri.EscapeDataString(stateToken);

        return "https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={_clientId}" +
               $"&redirect_uri={redirectUri}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&access_type=offline" +
               $"&prompt=consent" +
               $"&state={state}";
    }

    public async Task<TokenResponse> ExchangeCodeAsync(string code)
    {
        var flow = CreateFlow();
        return await flow.ExchangeCodeForTokenAsync("user", code, _redirectUri, CancellationToken.None);
    }

    public async Task SaveTokensAsync(int userId, TokenResponse token)
    {
        var existing = await _db.CalendarAccounts
            .FirstOrDefaultAsync(ca => ca.UserId == userId && ca.Provider == CalendarProvider.Google);

        if (existing != null)
        {
            existing.AccessToken = token.AccessToken;
            if (!string.IsNullOrEmpty(token.RefreshToken))
                existing.RefreshToken = token.RefreshToken;
            existing.TokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 3600);
            existing.IsConnected = true;
        }
        else
        {
            _db.CalendarAccounts.Add(new CalendarAccount
            {
                UserId = userId,
                Provider = CalendarProvider.Google,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken ?? "",
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 3600),
                IsConnected = true
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<CalendarEventDto>> GetEventsAsync(int userId, int daysAhead = 7)
    {
        var account = await _db.CalendarAccounts
            .FirstOrDefaultAsync(ca => ca.UserId == userId && ca.Provider == CalendarProvider.Google && ca.IsConnected);

        if (account == null) return [];

        var accessToken = await GetValidAccessTokenAsync(account);

        var credential = GoogleCredential.FromAccessToken(accessToken);
        var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Time Blocker"
        });

        var request = service.Events.List("primary");
        request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow;
        request.TimeMaxDateTimeOffset = DateTimeOffset.UtcNow.AddDays(daysAhead);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.MaxResults = 100;

        var events = await request.ExecuteAsync();

        return events.Items?
            .Select(e => new CalendarEventDto
            {
                Id = e.Id ?? "",
                Title = e.Summary ?? "(No title)",
                StartTime = e.Start?.DateTimeDateTimeOffset?.UtcDateTime
                            ?? DateTime.Parse((e.Start?.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd")) + "T00:00:00Z"),
                EndTime = e.End?.DateTimeDateTimeOffset?.UtcDateTime
                          ?? DateTime.Parse((e.End?.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd")) + "T00:00:00Z"),
                Description = e.Description,
                Location = e.Location,
                IsAllDay = e.Start?.DateTimeDateTimeOffset == null
            })
            .ToList() ?? [];
    }

    public async Task<bool> IsConnectedAsync(int userId)
    {
        return await _db.CalendarAccounts
            .AnyAsync(ca => ca.UserId == userId && ca.Provider == CalendarProvider.Google && ca.IsConnected);
    }

    private async Task<string> GetValidAccessTokenAsync(CalendarAccount account)
    {
        if (account.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
            return account.AccessToken;

        var flow = CreateFlow();
        var refreshed = await flow.RefreshTokenAsync("user", account.RefreshToken, CancellationToken.None);

        account.AccessToken = refreshed.AccessToken;
        account.TokenExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresInSeconds ?? 3600);
        await _db.SaveChangesAsync();

        return account.AccessToken;
    }

    private GoogleAuthorizationCodeFlow CreateFlow() =>
        new(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            },
            Scopes = [CalendarService.Scope.CalendarReadonly]
        });
}
