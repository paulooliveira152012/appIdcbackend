using Microsoft.EntityFrameworkCore;
using BibliaStudy.Api.Models;

namespace BibliaStudy.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<PointTransaction> PointTransactions { get; set; }
    public DbSet<DailyCheckIn> DailyCheckIns { get; set; }
    public DbSet<PointAward> PointAwards { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<NoteComment> NoteComments { get; set; }
    public DbSet<NoteLike> NoteLikes { get; set; }
}