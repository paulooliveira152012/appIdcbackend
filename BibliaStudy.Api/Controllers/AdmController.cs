using BibliaStudy.Api.Dtos;
using System.Threading.Tasks;
using BibliaStudy.Api.Data;
using BibliaStudy.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BibliaStudy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdmController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdmController(AppDbContext context)
    {
        _context = context;
    }

   [HttpGet("dataBasedOnDateChange")]
public async Task<IActionResult> DataBasedOnDateChange(
    [FromQuery] string userId,
    [FromQuery] string date,
    [FromQuery] string option,
    [FromQuery] string? service)
{
    Console.WriteLine("dataBasedOnDateChange");

    if (string.IsNullOrWhiteSpace(userId))
        return BadRequest(new { message = "falta userId" });

    if (string.IsNullOrWhiteSpace(date))
        return BadRequest(new { message = "falta date" });

    if (string.IsNullOrWhiteSpace(option))
        return BadRequest(new { message = "falta option" });

    if (!Guid.TryParse(userId, out var parsedUserId))
        return BadRequest(new { message = "userId inválido" });

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedUserId);

    if (user == null)
        return NotFound(new { message = "Usuário não encontrado" });

    if (string.IsNullOrWhiteSpace(user.Role) || user.Role.ToLower() != "leader")
        return StatusCode(403, new { message = "Apenas leaders podem acessar." });

    if (!DateTime.TryParse(date, out var parsedDate))
        return BadRequest(new { message = "date inválida" });

    var selectedDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
    var normalizedOption = option.Trim().ToLower();

    if (normalizedOption == "chamada")
    {
        if (string.IsNullOrWhiteSpace(service))
            return BadRequest(new { message = "falta service para chamada" });

        var normalizedService = service.Trim().ToLower();

        var attendances = await _context.Attendances
            .Where(a =>
                a.LocalAttendanceDate == selectedDate &&
                a.Service == normalizedService)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Service,
                a.Present,
                a.LocalAttendanceDate,
                a.MarkedByLeaderId,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            message = "Dados da chamada buscados com sucesso.",
            option = normalizedOption,
            service = normalizedService,
            selectedDate,
            attendances
        });
    }

    if (normalizedOption == "histórico de presença")
    {
        var history = await _context.Attendances
            .Where(a => a.LocalAttendanceDate == selectedDate)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Service,
                a.Present,
                a.LocalAttendanceDate,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            message = "Histórico buscado com sucesso.",
            option = normalizedOption,
            selectedDate,
            history
        });
    }

    if (normalizedOption == "pontuações")
    {
        // ajustar conforme sua tabela/model de pontos
        return Ok(new
        {
            message = "Pontuações buscadas com sucesso.",
            option = normalizedOption,
            selectedDate,
            scores = new List<object>()
        });
    }

    if (normalizedOption == "eventos")
    {
        // ajustar conforme sua tabela/model de eventos
        return Ok(new
        {
            message = "Eventos buscados com sucesso.",
            option = normalizedOption,
            selectedDate,
            events = new List<object>()
        });
    }

    return BadRequest(new { message = "option inválida" });
}

   [HttpPost("attendance")]
public async Task<IActionResult> Attendance([FromBody] AttendanceDto dto)
{
    try
    {
        if (dto == null)
            return BadRequest(new { message = "Dados inválidos." });

        if (string.IsNullOrWhiteSpace(dto.Service))
            return BadRequest(new { message = "Está faltando o tipo de serviço." });

        if (string.IsNullOrWhiteSpace(dto.LeaderUserId))
            return BadRequest(new { message = "Está faltando o userId do leader." });

        if (string.IsNullOrWhiteSpace(dto.UserId))
            return BadRequest(new { message = "Está faltando o userId do membro." });

        if (string.IsNullOrWhiteSpace(dto.Date))
            return BadRequest(new { message = "Está faltando a data." });

        if (!Guid.TryParse(dto.LeaderUserId, out var leaderUserId))
            return BadRequest(new { message = "LeaderUserId inválido." });

        if (!Guid.TryParse(dto.UserId, out var userId))
            return BadRequest(new { message = "UserId inválido." });

        if (!DateTime.TryParse(dto.Date, out var parsedDate))
            return BadRequest(new { message = "Data inválida." });

        var leader = await _context.Users.FirstOrDefaultAsync(u => u.Id == leaderUserId);
        if (leader == null)
            return NotFound(new { message = "Leader não encontrado." });

        if (string.IsNullOrWhiteSpace(leader.Role) || leader.Role.ToLower() != "leader")
            return StatusCode(403, new { message = "Apenas leaders podem realizar chamadas." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { message = "Usuário não encontrado." });

        var normalizedService = dto.Service.Trim().ToLower();
        var attendanceDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);

        var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(a =>
            a.UserId == userId &&
            a.Service == normalizedService &&
            a.LocalAttendanceDate == attendanceDate);

        if (existingAttendance != null)
        {
            existingAttendance.Present = dto.Present;
            existingAttendance.MarkedByLeaderId = leader.Id;
            existingAttendance.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = dto.Present
                    ? "Presença atualizada com sucesso."
                    : "Falta atualizada com sucesso."
            });
        }

        var nowUtc = DateTime.UtcNow;

        var attendance = new Attendance
        {
            Service = normalizedService,
            UserId = user.Id,
            MarkedByLeaderId = leader.Id,
            CreatedAt = nowUtc,
            LocalAttendanceDate = attendanceDate,
            Present = dto.Present
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = dto.Present
                ? "Presença registrada com sucesso."
                : "Falta registrada com sucesso.",
            attendance = new
            {
                id = attendance.Id,
                service = attendance.Service,
                userId = user.Id,
                username = user.Username,
                present = attendance.Present,
                markedByLeaderId = leader.Id,
                markedByLeaderUsername = leader.Username,
                createdAt = attendance.CreatedAt,
                localAttendanceDate = attendance.LocalAttendanceDate
            }
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = "Erro interno ao registrar presença.",
            error = ex.Message
        });
    }
}
}
