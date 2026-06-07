using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly GoogleCalendarService _googleCalendar;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _config;

    public CalendarController(GoogleCalendarService googleCalendar, JwtService jwtService, IConfiguration config)
    {
        _googleCalendar = googleCalendar;
        _jwtService = jwtService;
        _config = config;
    }

    // Returns the Google OAuth URL for the frontend to redirect to.
    [HttpGet("google/auth")]
    [Authorize]
    public IActionResult GetAuthUrl()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var stateToken = _jwtService.GenerateStateToken(userId);
        var authUrl = _googleCalendar.BuildAuthUrl(stateToken);
        return Ok(new { url = authUrl });
    }

    // Google redirects here after the user accepts permissions.
    [HttpGet("google/callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{_config["Frontend:BaseUrl"]}/settings?error={error}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest("Missing code or state.");

        var userId = _jwtService.ValidateStateToken(state);
        if (userId == null)
            return BadRequest("Invalid or expired state token.");

        var tokenResponse = await _googleCalendar.ExchangeCodeAsync(code);
        await _googleCalendar.SaveTokensAsync(userId.Value, tokenResponse);

        return Redirect($"{_config["Frontend:BaseUrl"]}/settings?connected=google");
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var connected = await _googleCalendar.IsConnectedAsync(userId);
        return Ok(new { connected });
    }

    [HttpGet("events")]
    [Authorize]
    public async Task<IActionResult> GetEvents([FromQuery] int daysAhead = 7)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var events = await _googleCalendar.GetEventsAsync(userId, daysAhead);
        return Ok(events);
    }
}
