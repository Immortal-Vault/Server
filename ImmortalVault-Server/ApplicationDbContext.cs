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
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSettings>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<User>(e => e.Id)
            .HasPrincipalKey<UserSettings>(e => e.UserId)
            .IsRequired();

        modelBuilder.Entity<UserSettings>()
            .Property(e => e.InactiveMinutes).HasDefaultValue(10).IsRequired();

        var builder = modelBuilder.Entity<User>();

        builder
            .HasOne(e => e.UserTokens)
            .WithOne()
            .HasForeignKey<UserTokens>(e => e.UserId)
            .HasPrincipalKey<User>(e => e.Id);

        builder.Property(e => e.MfaRecoveryCodes).HasColumnType("json").HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
        ).IsRequired(false);

        builder.Ignore(e => e.MfaEnabled);

        builder.Property(e => e.Password)
            .IsRequired();
    }
}