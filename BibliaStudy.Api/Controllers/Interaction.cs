using System.Threading.Tasks;
using BibliaStudy.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // para usar o AppDbContext
using BibliaStudy.Api.Dtos;
using BibliaStudy.Api.Models;



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


    [HttpPost("award-points")]
public async Task<IActionResult> AwardPoints([FromBody] AwardPointsDto dto)
{
    if (dto == null)
        return BadRequest(new { message = "Dados inválidos." });

    if (!Guid.TryParse(dto.LeaderUserId, out var leaderUserId))
        return BadRequest(new { message = "LeaderUserId inválido." });

    if (!Guid.TryParse(dto.TargetUserId, out var targetUserId))
        return BadRequest(new { message = "TargetUserId inválido." });

    if (dto.Points <= 0)
        return BadRequest(new { message = "A pontuação deve ser maior que zero." });

    if (dto.Points > 1000)
        return BadRequest(new { message = "Pontuação muito alta." });

    var leader = await _context.Users.FirstOrDefaultAsync(u => u.Id == leaderUserId);
    if (leader == null)
        return NotFound(new { message = "Leader não encontrado." });

    if (leader.Role?.ToLower() != "leader")
        return StatusCode(403, new { message = "Apenas leaders podem atribuir pontos." });

    var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
    if (targetUser == null)
        return NotFound(new { message = "Usuário de destino não encontrado." });

    var award = new PointAward
    {
        TargetUserId = targetUser.Id,
        GivenByUserId = leader.Id,
        Points = dto.Points,
        Reason = dto.Reason?.Trim() ?? string.Empty,
        CreatedAt = DateTime.UtcNow
    };

    targetUser.Points += dto.Points;

    // opcional: recalcular nível automaticamente
    targetUser.Level = (targetUser.Points / 100) + 1;

    _context.PointAwards.Add(award);
    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Pontos atribuídos com sucesso.",
        pointsAdded = dto.Points,
        reason = award.Reason,
        awardedAt = award.CreatedAt,
        targetUser = new
        {
            userId = targetUser.Id,
            username = targetUser.Username,
            email = targetUser.Email,
            profileImage = targetUser.ProfileImage,
            role = targetUser.Role,
            points = targetUser.Points,
            level = targetUser.Level,
            checkInStreak = targetUser.CheckInStreak,
            lastCheckInAt = targetUser.LastCheckInAt,
            createdAt = targetUser.CreatedAt
        }
    });
}

    
}