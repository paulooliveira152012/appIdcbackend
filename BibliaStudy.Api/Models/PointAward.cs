namespace BibliaStudy.Api.Models;

public class PointAward
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TargetUserId { get; set; }

    public Guid GivenByUserId { get; set; }

    public int Points { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}