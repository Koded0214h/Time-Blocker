namespace backend.Dtos;

public class CalendarEventDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsAllDay { get; set; }
}
