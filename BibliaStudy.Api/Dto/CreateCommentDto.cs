namespace BibliaStudy.Api.DTOs;

public class CreateCommentDto
{
    public string UserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}