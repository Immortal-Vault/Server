using System.Text.Json;
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
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

        builder.Property(e => e.MfaRecoveryCodes).HasColumnType("json").HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
        ).IsRequired(false);

        builder.Ignore(e => e.MfaEnabled);

        builder.Property(e => e.Password)
            .IsRequired();
    }
}