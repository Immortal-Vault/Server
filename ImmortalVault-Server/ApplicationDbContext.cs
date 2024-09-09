using ImmortalVault_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server;

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
}