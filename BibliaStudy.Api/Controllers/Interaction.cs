using System.Threading.Tasks;
using BibliaStudy.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // para usar o AppDbContext


namespace BibliaStudy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InteractionController : ControllerBase
{
    private readonly AppDbContext _context;

    public InteractionController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("checkin")]
    public async Task<IActionResult> Checkin([FromBody] CheckinDto dto)
    {
        Console.WriteLine($"Checking in... {dto?.userId}");

        if (dto == null || string.IsNullOrWhiteSpace(dto.userId))
            return BadRequest(new { message = "UserId é obrigatório." });

        if (!Guid.TryParse(dto.userId, out var userId))
            return BadRequest(new { message = "UserId inválido." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "Usuário não encontrado." });

        var now = DateTime.UtcNow;
        var today = now.Date;
        var lastCheckInDate = user.LastCheckInAt?.Date;

        if (lastCheckInDate == today)
        {
            return BadRequest(new
            {
                message = "Check-in já realizado hoje.",
                streak = user.CheckInStreak ?? 0,
                points = user.Points,
                lastCheckInAt = user.LastCheckInAt
            });
        }

        var yesterday = today.AddDays(-1);

        if (lastCheckInDate == yesterday)
        {
            user.CheckInStreak = (user.CheckInStreak ?? 0) + 1;
        }
        else
        {
            user.CheckInStreak = 1;
        }

        var basePoints = 10;
        var bonusPoints = 0;

        if (user.CheckInStreak == 3)
            bonusPoints = 10;
        else if (user.CheckInStreak == 7)
            bonusPoints = 25;
        else if (user.CheckInStreak == 15)
            bonusPoints = 50;
        else if (user.CheckInStreak == 30)
            bonusPoints = 100;

        var totalPointsEarned = basePoints + bonusPoints;

        user.Points += totalPointsEarned;
        user.LastCheckInAt = now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Check-in realizado com sucesso.",
            pointsEarned = totalPointsEarned,
            basePoints,
            bonusPoints,
            streak = user.CheckInStreak,
            points = user.Points,
            user = new
            {
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                profileImage = user.ProfileImage,
                role = user.Role,
                points = user.Points,
                level = user.Level,
                checkInStreak = user.CheckInStreak,
                lastCheckInAt = user.LastCheckInAt,
                createdAt = user.CreatedAt
            }
        });
    }
}