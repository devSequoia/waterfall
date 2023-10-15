using waterfall.Contexts.Content;
using Microsoft.EntityFrameworkCore;

namespace waterfall.Contexts;

public partial class ActivityHistoryDb(IConfiguration configuration) : DbContext
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
            entity.HasKey(e => e.Id).HasName("acthist_pkey");

            entity.ToTable("activityhistory");

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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
