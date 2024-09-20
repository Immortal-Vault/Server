using ImmortalVault_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server;

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserLocalization> UsersLocalizations { get; set; } = null!;
    public DbSet<UserTokens> UsersTokens { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne(e => e.UserLocalization)
            .WithOne(e => e.User)
            .HasForeignKey<UserLocalization>(e => e.UserId);
        
        modelBuilder.Entity<User>()
            .HasOne(e => e.UserTokens)
            .WithOne(e => e.User)
            .HasForeignKey<UserTokens>(e => e.UserId);
    }
}