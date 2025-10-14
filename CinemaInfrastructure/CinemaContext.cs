using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CinemaDomain.Model;

namespace CinemaInfrastructure
{
    public class CinemaContext : IdentityDbContext<User>
    {
        public CinemaContext(DbContextOptions<CinemaContext> options)
            : base(options)
        {
        }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Film> Films { get; set; }
        public DbSet<FilmCategory> FilmCategories { get; set; }
        public DbSet<FilmRating> FilmRatings { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<HallType> HallTypes { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Viewer> Viewers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => new { e.ViewerId, e.SessionId, e.SeatId });

                entity.HasOne(d => d.Seat).WithMany(p => p.Bookings)
                      .HasForeignKey(d => d.SeatId)
                      .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Session).WithMany(p => p.Bookings)
                      .HasForeignKey(d => d.SessionId)
                      .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Viewer).WithMany(p => p.Bookings)
                      .HasForeignKey(d => d.ViewerId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(30);
            });

            modelBuilder.Entity<Film>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(150);
                entity.Property(e => e.Name).HasMaxLength(40).IsRequired();
                entity.Property(e => e.PosterPath).HasMaxLength(255).IsUnicode();

                entity.HasOne(d => d.Company).WithMany(p => p.Films)
                      .HasForeignKey(d => d.CompanyId)
                      .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.FilmCategory).WithMany(p => p.Films)
                      .HasForeignKey(d => d.FilmCategoryId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<FilmCategory>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(30);
            });

            modelBuilder.Entity<FilmRating>(entity =>
            {
                entity.HasOne(d => d.Film).WithMany(p => p.FilmRatings)
                      .HasForeignKey(d => d.FilmId)
                      .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Viewer).WithMany(p => p.FilmRatings)
                      .HasForeignKey(d => d.ViewerId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Hall>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(30);

                entity.HasOne(d => d.HallType).WithMany(p => p.Halls)
                      .HasForeignKey(d => d.HallTypeId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<HallType>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(30);
            });

            modelBuilder.Entity<Seat>(entity =>
            {
                entity.HasOne(d => d.Hall).WithMany(p => p.Seats)
                      .HasForeignKey(d => d.HallId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.Property(e => e.SessionTime).HasColumnType("datetime");

                entity.HasOne(d => d.Film).WithMany(p => p.Sessions)
                      .HasForeignKey(d => d.FilmId)
                      .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Hall).WithMany(p => p.Sessions)
                      .HasForeignKey(d => d.HallId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Viewer>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(30);
                entity.Property(e => e.DateOfBirth)
                      .HasColumnType("date");

                entity.HasOne(v => v.User)
                      .WithOne(u => u.Viewer)
                      .HasForeignKey<Viewer>(v => v.UserId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });
        }
    }
}
