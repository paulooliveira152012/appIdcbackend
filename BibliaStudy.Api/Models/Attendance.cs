using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliaStudy.Api.Models;

public class Attendance
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Service { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public Guid MarkedByLeaderId { get; set; }

    [ForeignKey(nameof(MarkedByLeaderId))]
    public User? MarkedByLeader { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LocalAttendanceDate { get; set; }

    [Required]
    public bool Present { get; set; }
}