using Microsoft.EntityFrameworkCore;
using waterfall.Contexts.Content;

namespace waterfall.Contexts;

public partial class PlayerDb(IConfiguration configuration) : DbContext
{
    private readonly string? _connectionString = configuration.GetConnectionString("PostgreSQLDb");

    public virtual DbSet<Player> Players { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_connectionString is null)
            throw new Exception("Connection string is null");

        optionsBuilder.UseNpgsql(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.MembershipId).HasName("players_pkey");

            entity.ToTable("players");

            entity.HasIndex(e => e.MembershipId, "players_membershipId_key").IsUnique();

            entity.Property(e => e.MembershipId)
                .ValueGeneratedNever()
                .HasColumnName("membershipId");
            entity.Property(e => e.BanReason).HasColumnName("banReason");
            entity.Property(e => e.BanTime).HasColumnName("banTime");
            entity.Property(e => e.IsBanned).HasColumnName("isBanned");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
