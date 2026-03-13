using System.Threading.Tasks;
using BibliaStudy.Api.Data;
using BibliaStudy.Api.DTOs;
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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    private string GenerateJwtToken(User user)
    {
        var key = Environment.GetEnvironmentVariable("JWT_KEY");
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new Exception("JWT_KEY não foi encontrada.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("signup")]
    public IActionResult Signup([FromBody] RegisterUserDto dto)
    {
        Console.WriteLine("signup...");
        try
        {
            if (dto == null)
                return BadRequest(new { message = "Dados inválidos." });

            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.Username))
            {
                return BadRequest(new { message = "Email, senha e username são obrigatórios." });
            }

            // verificar se email já existe
            var emailExists = _context.Users.Any(u => u.Email == dto.Email);

            if (emailExists)
            {
                return Conflict(new { message = "Este email já está registrado." });
            }

            // receber o profileImage e salvar no banco (garantir que tenha no modelo)
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                ProfileImage = dto.ProfileImage,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Conta criada com sucesso",
                token,
                user = new
                {
                    userId = user.Id,
                    username = user.Username,
                    email = user.Email,
                    profileImage = user.ProfileImage,
                    role = user.Role,
                    points = user.Points,
                    level = user.Level,
                    checkinStreak = user.CheckInStreak,
                },

            });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, new
            {
                message = "Erro ao salvar usuário no banco.",
                error = dbEx.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Erro interno do servidor.",
                error = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        Console.WriteLine($"email: {dto.Email}");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Email ou senha inválidos"
            });
        }

        var passwordIsValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!passwordIsValid)
        {
            return Unauthorized(new
            {
                message = "Email ou senha inválidos"
            });
        }

        var token = GenerateJwtToken(user);

        return Ok(new
        {
            message = "Login realizado com sucesso",
            token,
            user = new
            {
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                profileImage = user.ProfileImage,

                level = user.Level,
                points = user.Points,
                lastCheckInAt = user.LastCheckInAt,
                checkInStreak = user.CheckInStreak,
                role = user.Role
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        if (!Guid.TryParse(id, out var userId))
            return BadRequest(new { message = "Id inválido." });

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                userId = u.Id,
                username = u.Username,
                email = u.Email,
                profileImage = u.ProfileImage,
                level = u.Level,
                points = u.Points,
                lastCheckInAt = u.LastCheckInAt,
                checkInStreak = u.CheckInStreak,
                role = u.Role
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "Usuário não encontrado." });

        return Ok(new { user });
    }


    [HttpPut("update-profile")]
    public async Task<IActionResult> ProfileUpdate([FromBody] UpdateProfileDto dto)
    {
        Console.WriteLine("update-profile");
        if (dto == null)
            return BadRequest("Dados inválidos.");

        if (string.IsNullOrWhiteSpace(dto.UserId))
            return BadRequest("UserId é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Username))
            return BadRequest("Username é obrigatório.");

        if (!Guid.TryParse(dto.UserId, out var userId))
            return BadRequest("UserId inválido.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound("Usuário não encontrado.");

        user.Username = dto.Username.Trim();
        user.ProfileImage = string.IsNullOrWhiteSpace(dto.ProfileImage)
            ? user.ProfileImage
            : dto.ProfileImage.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Perfil atualizado com sucesso",
            user
        });
    }


    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .Select(u => new
            {
                userId = u.Id,
                username = u.Username,
                profileImage = u.ProfileImage,
                role = u.Role,
                points = u.Points,
                level = u.Level,
                checkInStreak = u.CheckInStreak,
                lastCheckInAt = u.LastCheckInAt,
                createdAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }



    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "API funcionando" });
    }
}