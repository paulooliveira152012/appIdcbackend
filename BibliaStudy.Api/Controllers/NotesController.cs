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
                isHighlighted = n.IsHighlighted,
                createdBy = new
                {
                    userId = n.CreatedBy!.Id,
                    username = n.CreatedBy.Username,
                    profileImage = n.CreatedBy.ProfileImage,
                    level = n.CreatedBy.Level
                },
                likesCount = n.Likes.Count,
                commentsCount = n.Comments.Count,
                likes = n.Likes.Select(l => new
                {
                    userId = l.UserId,
                    username = l.User!.Username,
                    profileImage = l.User.ProfileImage
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

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(string commentId, [FromQuery] string userId)
    {
        if (!Guid.TryParse(commentId, out var parsedCommentId))
            return BadRequest(new { message = "CommentId inválido." });

        if (!Guid.TryParse(userId, out var parsedUserId))
            return BadRequest(new { message = "UserId inválido." });

        var comment = await _context.NoteComments.FirstOrDefaultAsync(c => c.Id == parsedCommentId);

        if (comment == null)
            return NotFound(new { message = "Comentário não encontrado." });

        if (comment.UserId != parsedUserId)
            return StatusCode(403, new { message = "Apenas o dono do comentário pode apagá-lo." });

        _context.NoteComments.Remove(comment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Comentário apagado com sucesso." });
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



    [HttpPost("highlight")]
    public async Task<IActionResult> SetHighlightedNote([FromBody] HighlightNoteDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { message = "Dados inválidos." });

            if (string.IsNullOrWhiteSpace(dto.LeaderUserId))
                return BadRequest(new { message = "LeaderUserId é obrigatório." });

            if (string.IsNullOrWhiteSpace(dto.NoteId))
                return BadRequest(new { message = "NoteId é obrigatório." });

            if (!Guid.TryParse(dto.LeaderUserId, out var leaderUserId))
                return BadRequest(new { message = "LeaderUserId inválido." });

            if (!Guid.TryParse(dto.NoteId, out var noteId))
                return BadRequest(new { message = "NoteId inválido." });

            var leader = await _context.Users.FirstOrDefaultAsync(u => u.Id == leaderUserId);
            if (leader == null)
                return NotFound(new { message = "Leader não encontrado." });

            if (string.IsNullOrWhiteSpace(leader.Role) || leader.Role.ToLower() != "leader")
                return StatusCode(403, new { message = "Apenas leaders podem destacar anotações." });

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note == null)
                return NotFound(new { message = "Anotação não encontrada." });

            var highlightedNotes = await _context.Notes
                .Where(n => n.IsHighlighted)
                .ToListAsync();

            foreach (var item in highlightedNotes)
            {
                item.IsHighlighted = false;
            }

            note.IsHighlighted = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Anotação destacada com sucesso.",
                noteId = note.Id
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Erro interno ao destacar anotação.",
                error = ex.Message
            });
        }
    }

    [HttpGet("highlighted")]
    public async Task<IActionResult> GetHighlightedNote()
    {
        try
        {
            var note = await _context.Notes
        .Where(n => n.IsHighlighted)
        .Select(n => new
        {
            id = n.Id,
            title = n.Title,
            content = n.Content,
            createdAt = n.CreatedAt,
            isHighlighted = n.IsHighlighted,

            createdBy = new
            {
                userId = n.CreatedBy!.Id,
                username = n.CreatedBy.Username,
                profileImage = n.CreatedBy.ProfileImage
            }
        })
        .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Anotação destacada buscada com sucesso.",
                note
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Erro interno ao buscar anotação destacada.",
                error = ex.Message
            });
        }
    }
}