namespace BibliaStudy.Api.Models;

public class DailyCheckIn
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CheckInDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}