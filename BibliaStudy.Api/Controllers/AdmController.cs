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

    [HttpGet("attendance/by-date-and-service")]
    public async Task<IActionResult> GetAttendanceByDateAndService(
        [FromQuery] string leaderUserId,
        [FromQuery] string date,
        [FromQuery] string service)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(leaderUserId))
                return BadRequest(new { message = "leaderUserId é obrigatório." });

            if (string.IsNullOrWhiteSpace(date))
                return BadRequest(new { message = "date é obrigatório." });

            if (string.IsNullOrWhiteSpace(service))
                return BadRequest(new { message = "service é obrigatório." });

            if (!Guid.TryParse(leaderUserId, out var parsedLeaderId))
                return BadRequest(new { message = "leaderUserId inválido." });

            var leader = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedLeaderId);
            if (leader == null)
                return NotFound(new { message = "Leader não encontrado." });

            if (string.IsNullOrWhiteSpace(leader.Role) || leader.Role.ToLower() != "leader")
                return StatusCode(403, new { message = "Apenas leaders podem acessar." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest(new { message = "Data inválida." });

            var normalizedService = service.Trim().ToLower();
            var attendanceDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);

            var attendances = await _context.Attendances
                .Where(a =>
                    a.LocalAttendanceDate == attendanceDate &&
                    a.Service == normalizedService)
                .Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    service = a.Service,
                    present = a.Present,
                    localAttendanceDate = a.LocalAttendanceDate,
                    markedByLeaderId = a.MarkedByLeaderId,
                    createdAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Chamada buscada com sucesso.",
                service = normalizedService,
                date = attendanceDate,
                attendances
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Erro interno ao buscar chamada.",
                error = ex.Message
            });
        }
    }

    [HttpGet("attendance/history")]
    public async Task<IActionResult> GetAttendanceHistory(
    [FromQuery] string leaderUserId,
    [FromQuery] string targetUserId,
    [FromQuery] string periodType,
    [FromQuery] int year,
    [FromQuery] int? month,
    [FromQuery] int? quarter,
    [FromQuery] int? semester)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(leaderUserId))
                return BadRequest(new { message = "leaderUserId é obrigatório." });

            if (string.IsNullOrWhiteSpace(targetUserId))
                return BadRequest(new { message = "targetUserId é obrigatório." });

            if (string.IsNullOrWhiteSpace(periodType))
                return BadRequest(new { message = "periodType é obrigatório." });

            if (!Guid.TryParse(leaderUserId, out var parsedLeaderId))
                return BadRequest(new { message = "leaderUserId inválido." });

            if (!Guid.TryParse(targetUserId, out var parsedTargetUserId))
                return BadRequest(new { message = "targetUserId inválido." });

            var leader = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedLeaderId);
            if (leader == null)
                return NotFound(new { message = "Leader não encontrado." });

            if (string.IsNullOrWhiteSpace(leader.Role) || leader.Role.ToLower() != "leader")
                return StatusCode(403, new { message = "Apenas leaders podem acessar." });

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedTargetUserId);
            if (targetUser == null)
                return NotFound(new { message = "Usuário alvo não encontrado." });

            var normalizedPeriodType = periodType.Trim().ToLower();

            DateTime startDate;
            DateTime endDate;

            switch (normalizedPeriodType)
            {
                case "month":
                    if (!month.HasValue || month < 1 || month > 12)
                        return BadRequest(new { message = "month inválido." });

                    startDate = new DateTime(year, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(1);
                    break;

                case "quarter":
                    if (!quarter.HasValue || quarter < 1 || quarter > 4)
                        return BadRequest(new { message = "quarter inválido." });

                    var quarterStartMonth = (quarter.Value - 1) * 3 + 1;
                    startDate = new DateTime(year, quarterStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(3);
                    break;

                case "semester":
                    if (!semester.HasValue || (semester != 1 && semester != 2))
                        return BadRequest(new { message = "semester inválido." });

                    var semesterStartMonth = semester.Value == 1 ? 1 : 7;
                    startDate = new DateTime(year, semesterStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(6);
                    break;

                case "year":
                    startDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddYears(1);
                    break;

                default:
                    return BadRequest(new { message = "periodType inválido. Use month, quarter, semester ou year." });
            }

            var attendances = await _context.Attendances
                .Where(a =>
                    a.UserId == parsedTargetUserId &&
                    a.LocalAttendanceDate >= startDate &&
                    a.LocalAttendanceDate < endDate)
                .OrderByDescending(a => a.LocalAttendanceDate)
                .Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    service = a.Service,
                    present = a.Present,
                    localAttendanceDate = a.LocalAttendanceDate,
                    createdAt = a.CreatedAt,
                    markedByLeaderId = a.MarkedByLeaderId
                })
                .ToListAsync();

            var total = attendances.Count;
            var presents = attendances.Count(a => a.present);
            var absents = total - presents;
            var attendanceRate = total == 0 ? 0 : Math.Round((double)presents / total * 100, 2);

            return Ok(new
            {
                message = "Histórico de presença buscado com sucesso.",
                user = new
                {
                    userId = targetUser.Id,
                    username = targetUser.Username,
                    profileImage = targetUser.ProfileImage,
                    role = targetUser.Role
                },
                period = new
                {
                    periodType = normalizedPeriodType,
                    year,
                    month,
                    quarter,
                    semester,
                    startDate,
                    endDate
                },
                summary = new
                {
                    total,
                    presents,
                    absents,
                    attendanceRate
                },
                attendances
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Erro interno ao buscar histórico de presença.",
                error = ex.Message
            });
        }
    }

    [HttpGet("attendance/history/summary")]
    public async Task<IActionResult> GetAttendanceHistorySummary(
    [FromQuery] string leaderUserId,
    [FromQuery] string periodType,
    [FromQuery] int year,
    [FromQuery] int? month,
    [FromQuery] int? quarter,
    [FromQuery] int? semester)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(leaderUserId))
                return BadRequest(new { message = "leaderUserId é obrigatório." });

            if (string.IsNullOrWhiteSpace(periodType))
                return BadRequest(new { message = "periodType é obrigatório." });

            if (!Guid.TryParse(leaderUserId, out var parsedLeaderId))
                return BadRequest(new { message = "leaderUserId inválido." });

            var leader = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedLeaderId);
            if (leader == null)
                return NotFound(new { message = "Leader não encontrado." });

            if (string.IsNullOrWhiteSpace(leader.Role) || leader.Role.ToLower() != "leader")
                return StatusCode(403, new { message = "Apenas leaders podem acessar." });

            var normalizedPeriodType = periodType.Trim().ToLower();

            DateTime startDate;
            DateTime endDate;

            switch (normalizedPeriodType)
            {
                case "month":
                    if (!month.HasValue || month < 1 || month > 12)
                        return BadRequest(new { message = "month inválido." });

                    startDate = new DateTime(year, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(1);
                    break;

                case "quarter":
                    if (!quarter.HasValue || quarter < 1 || quarter > 4)
                        return BadRequest(new { message = "quarter inválido." });

                    var quarterStartMonth = (quarter.Value - 1) * 3 + 1;
                    startDate = new DateTime(year, quarterStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(3);
                    break;

                case "semester":
                    if (!semester.HasValue || (semester != 1 && semester != 2))
                        return BadRequest(new { message = "semester inválido." });

                    var semesterStartMonth = semester.Value == 1 ? 1 : 7;
                    startDate = new DateTime(year, semesterStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(6);
                    break;

                case "year":
                    startDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddYears(1);
                    break;

                default:
                    return BadRequest(new { message = "periodType inválido. Use month, quarter, semester ou year." });
            }

            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.ProfileImage,
                    u.Role
                })
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.LocalAttendanceDate >= startDate && a.LocalAttendanceDate < endDate)
                .ToListAsync();

            var summary = users
                .Select(u =>
                {
                    var userAttendances = attendances.Where(a => a.UserId == u.Id).ToList();
                    var total = userAttendances.Count;
                    var presents = userAttendances.Count(a => a.Present);
                    var absents = total - presents;
                    var attendanceRate = total == 0 ? 0 : Math.Round((double)presents / total * 100, 2);

                    return new
                    {
                        userId = u.Id,
                        username = u.Username,
                        profileImage = u.ProfileImage,
                        role = u.Role,
                        total,
                        presents,
                        absents,
                        attendanceRate
                    };
                })
                .OrderByDescending(x => x.attendanceRate)
                .ThenByDescending(x => x.presents)
                .ToList();

            return Ok(new
            {
                message = "Resumo do histórico buscado com sucesso.",
                period = new
                {
                    periodType = normalizedPeriodType,
                    year,
                    month,
                    quarter,
                    semester,
                    startDate,
                    endDate
                },
                summary
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Erro interno ao buscar resumo do histórico.",
                error = ex.Message
            });
        }
    }
}
