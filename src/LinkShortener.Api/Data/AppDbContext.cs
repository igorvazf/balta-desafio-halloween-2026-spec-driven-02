using LinkShortener.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LinkShortener.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const int CodeMaxLength = 20;
    public const int TargetUrlMaxLength = 2048;

    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // O SQLite não guarda o Kind da data; tudo que entra e sai do banco é UTC.
        var utc = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var link = modelBuilder.Entity<Link>();

        link.ToTable("Links");
        link.HasKey(l => l.Id);
        link.Property(l => l.Code).IsRequired().HasMaxLength(CodeMaxLength);
        link.Property(l => l.TargetUrl).IsRequired().HasMaxLength(TargetUrlMaxLength);
        link.Property(l => l.CreatedAtUtc).IsRequired().HasConversion(utc);
        link.Property(l => l.ExpiresAtUtc).HasConversion(utc);
        link.Property(l => l.AccessCount).IsRequired().HasDefaultValue(0L);

        // D-04: a unicidade do código é garantida pelo banco, não só pelo serviço.
        link.HasIndex(l => l.Code).IsUnique();
    }
}
