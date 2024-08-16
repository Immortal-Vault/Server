using ImmortalVault_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server;

public sealed class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
    }
}