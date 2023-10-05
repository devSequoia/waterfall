using Microsoft.EntityFrameworkCore;

namespace waterfall.DbContexts;

public class PlayerDb(IConfiguration configuration) : DbContext
{
        private readonly string? _connectionString = configuration.GetConnectionString("PostgreSQLDb");

        public virtual DbSet<User> Players { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_connectionString is null)
                throw new Exception("Connection string is null");

            optionsBuilder.UseNpgsql(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                // ReSharper disable StringLiteralTypo
                entity.HasKey(e => e.Id).HasName("acthist_moons_pkey");

                entity.ToTable("players");

                entity.Property(e => e.Id)
                    .UseIdentityAlwaysColumn()
                    .HasColumnName("id");
                entity.Property(e => e.MembershipId).HasColumnName("membershipId");
                entity.Property(e => e.BungieName).HasColumnName("bungieName");
                entity.Property(e => e.IsBanned).HasColumnName("isBanned");
                entity.Property(e => e.BanReason).HasColumnName("banReason");
                entity.Property(e => e.BanTime).HasColumnName("banTime");
                // ReSharper restore StringLiteralTypo
            });
        }
}

public class User
{
    public int Id { get; set; }
    public long MembershipId { get; set; }
    public string BungieName { get; set; } = null!;
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BanTime { get; set; }
}