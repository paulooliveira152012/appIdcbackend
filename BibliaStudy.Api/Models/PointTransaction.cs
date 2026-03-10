namespace BibliaStudy.Api.Models;

public class PointTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int Points { get; set; }

    public string Reason { get; set; } = string.Empty;
    // exemplos: "daily_checkin", "quiz_correct_answer", "quiz_completed"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}