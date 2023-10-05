using Microsoft.EntityFrameworkCore;

namespace waterfall.DbContexts;

public class ActivityHistoryDb(IConfiguration configuration) : DbContext
{
    private readonly string? _connectionString = configuration.GetConnectionString("PostgreSQLDb");

    public virtual DbSet<Activity> Activities { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_connectionString is null)
            throw new Exception("Connection string is null");

        optionsBuilder.UseNpgsql(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>(entity =>
        {
            // ReSharper disable StringLiteralTypo
            entity.HasKey(e => e.Id).HasName("acthist_moons_pkey");

            entity.ToTable("acthist_moons");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.MembershipId).HasColumnName("membershipId");
            entity.Property(e => e.ActivityHash).HasColumnName("activityHash");
            entity.Property(e => e.InstanceId).HasColumnName("instanceId");
            entity.Property(e => e.IsCompleted).HasColumnName("isCompleted");
            entity.Property(e => e.Time)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("time");
            // ReSharper restore StringLiteralTypo
        });
    }
}

public class Activity
{
    public int Id { get; set; }
    public long MembershipId { get; set; }
    public DateTime Time { get; set; }
    public long ActivityHash { get; set; }
    public long InstanceId { get; set; }
    public bool IsCompleted { get; set; }
}