namespace BibliaStudy.Api.Models;

public class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string ProfileImage { get; set; } = string.Empty;

    public string Role { get; set; } = "user";

    // pontos acumulados no app
    public int Points { get; set; } = 0;

    // nível atual do usuário
    public int Level { get; set; } = 1;

    // sequência de check-ins diários
    public int? CheckInStreak { get; set; } = 0;

    // data do último check-in
    public DateTime? LastCheckInAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}