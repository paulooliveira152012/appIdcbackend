namespace BibliaStudy.Api.DTOs;

public class UpdateNoteDto
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Tag { get; set; } = "outro";
}