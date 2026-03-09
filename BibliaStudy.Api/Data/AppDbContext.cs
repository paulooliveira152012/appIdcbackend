using Microsoft.EntityFrameworkCore;
using BibliaStudy.Api.Models;

namespace BibliaStudy.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}