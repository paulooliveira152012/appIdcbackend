using BibliaStudy.Api.Data;
using BibliaStudy.Api.DTOs;
using BibliaStudy.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliaStudy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notes = await _context.Notes
            .Include(n => n.CreatedBy)
            .Include(n => n.Likes)
            .Include(n => n.Comments)
                .ThenInclude(c => c.User)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                id = n.Id,
                title = n.Title,
                content = n.Content,
                tag = n.Tag,
                createdAt = n.CreatedAt,
                updatedAt = n.UpdatedAt,
                createdBy = new
                {
                    userId = n.CreatedBy!.Id,
                    username = n.CreatedBy.Username,
                    profileImage = n.CreatedBy.ProfileImage
                },
                likesCount = n.Likes.Count,
                commentsCount = n.Comments.Count,
                likes = n.Likes.Select(l => new
                {
                    userId = l.UserId
                }),
                comments = n.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        id = c.Id,
                        text = c.Text,
                        createdAt = c.CreatedAt,
                        user = new
                        {
                            userId = c.User!.Id,
                            username = c.User.Username,
                            profileImage = c.User.ProfileImage
                        }
                    })
            })
            .ToListAsync();

        return Ok(notes);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
    {
        if (!Guid.TryParse(dto.UserId, out var userId))
            return BadRequest(new { message = "UserId inválido." });

        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Título e conteúdo são obrigatórios." });

        var allowedTags = new[] { "culto domingo", "culto de ensino", "escola dominical", "outro" };
        var tag = allowedTags.Contains(dto.Tag.Trim().ToLower()) ? dto.Tag.Trim().ToLower() : "outro";

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "Usuário não encontrado." });

        var note = new Note
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            Tag = tag,
            CreatedById = user.Id
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Anotação criada com sucesso.",
            note = new
            {
                id = note.Id,
                title = note.Title,
                content = note.Content,
                tag = note.Tag,
                createdAt = note.CreatedAt,
                createdBy = new
                {
                    userId = user.Id,
                    username = user.Username,
                    profileImage = user.ProfileImage
                },
                likesCount = 0,
                commentsCount = 0,
                likes = new List<object>(),
                comments = new List<object>()
            }
        });
    }

    [HttpPut("{noteId}")]
    public async Task<IActionResult> Update(string noteId, [FromBody] UpdateNoteDto dto)
    {
        if (!Guid.TryParse(noteId, out var parsedNoteId))
            return BadRequest(new { message = "NoteId inválido." });

        if (!Guid.TryParse(dto.UserId, out var userId))
            return BadRequest(new { message = "UserId inválido." });

        var note = await _context.Notes
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == parsedNoteId);

        if (note == null)
            return NotFound(new { message = "Anotação não encontrada." });

        if (note.CreatedById != userId)
            return StatusCode(403, new { message = "Apenas o dono da anotação pode editar." });

        note.Title = dto.Title.Trim();
        note.Content = dto.Content.Trim();
        note.Tag = dto.Tag.Trim().ToLower();
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Anotação atualizada com sucesso."
        });
    }

    [HttpDelete("{noteId}")]
    public async Task<IActionResult> Delete(string noteId, [FromQuery] string userId)
    {
        if (!Guid.TryParse(noteId, out var parsedNoteId))
            return BadRequest(new { message = "NoteId inválido." });

        if (!Guid.TryParse(userId, out var parsedUserId))
            return BadRequest(new { message = "UserId inválido." });

        var note = await _context.Notes
            .Include(n => n.Comments)
            .Include(n => n.Likes)
            .FirstOrDefaultAsync(n => n.Id == parsedNoteId);

        if (note == null)
            return NotFound(new { message = "Anotação não encontrada." });

        if (note.CreatedById != parsedUserId)
            return StatusCode(403, new { message = "Apenas o dono da anotação pode deletar." });

        _context.NoteComments.RemoveRange(note.Comments);
        _context.NoteLikes.RemoveRange(note.Likes);
        _context.Notes.Remove(note);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Anotação deletada com sucesso." });
    }

    [HttpPost("{noteId}/comments")]
    public async Task<IActionResult> AddComment(string noteId, [FromBody] CreateCommentDto dto)
    {
        if (!Guid.TryParse(noteId, out var parsedNoteId))
            return BadRequest(new { message = "NoteId inválido." });

        if (!Guid.TryParse(dto.UserId, out var userId))
            return BadRequest(new { message = "UserId inválido." });

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { message = "Comentário obrigatório." });

        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == parsedNoteId);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (note == null)
            return NotFound(new { message = "Anotação não encontrada." });

        if (user == null)
            return NotFound(new { message = "Usuário não encontrado." });

        var comment = new NoteComment
        {
            NoteId = note.Id,
            UserId = user.Id,
            Text = dto.Text.Trim()
        };

        _context.NoteComments.Add(comment);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Comentário adicionado com sucesso.",
            comment = new
            {
                id = comment.Id,
                text = comment.Text,
                createdAt = comment.CreatedAt,
                user = new
                {
                    userId = user.Id,
                    username = user.Username,
                    profileImage = user.ProfileImage
                }
            }
        });
    }

    [HttpPost("{noteId}/likes")]
    public async Task<IActionResult> ToggleLike(string noteId, [FromBody] ToggleLikeDto dto)
    {
        if (!Guid.TryParse(noteId, out var parsedNoteId))
            return BadRequest(new { message = "NoteId inválido." });

        if (!Guid.TryParse(dto.UserId, out var userId))
            return BadRequest(new { message = "UserId inválido." });

        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == parsedNoteId);
        if (note == null)
            return NotFound(new { message = "Anotação não encontrada." });

        var existingLike = await _context.NoteLikes
            .FirstOrDefaultAsync(l => l.NoteId == parsedNoteId && l.UserId == userId);

        if (existingLike != null)
        {
            _context.NoteLikes.Remove(existingLike);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Like removido com sucesso.", liked = false });
        }

        var like = new NoteLike
        {
            NoteId = parsedNoteId,
            UserId = userId
        };

        _context.NoteLikes.Add(like);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Like adicionado com sucesso.", liked = true });
    }
}