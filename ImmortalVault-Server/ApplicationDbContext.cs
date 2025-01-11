using ImmortalVault_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server;

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserSettings> UsersSettings { get; set; } = null!;
    public DbSet<UserTokens> UsersTokens { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<User>();
        builder
            .HasOne(e => e.UserSettings)
            .WithOne(e => e.User)
            .HasForeignKey<UserSettings>(e => e.UserId);

        builder
            .HasOne(e => e.UserTokens)
            .WithOne(e => e.User)
            .HasForeignKey<UserTokens>(e => e.UserId);

        builder.Property(e => e.MfaRecoveryCodes).HasColumnType("json").IsRequired(false);

        builder.Ignore(e => e.MfaEnabled);

        builder.Property(e => e.Password)
            .IsRequired();
    }
}