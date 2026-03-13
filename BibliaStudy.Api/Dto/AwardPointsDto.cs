namespace BibliaStudy.Api.Dtos;

public class AwardPointsDto
{
    public string LeaderUserId { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
}