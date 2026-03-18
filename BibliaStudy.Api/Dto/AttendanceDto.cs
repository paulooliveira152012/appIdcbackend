namespace BibliaStudy.Api.Dtos;

public class AttendanceDto
{
    public string Service { get; set; } = "";
    public string LeaderUserId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string TimeZone { get; set; } = "UTC";
    public string Date { get; set; } = "";
    public bool Present { get; set; }
}