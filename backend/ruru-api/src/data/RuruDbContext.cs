using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ruru_api.model;

namespace ruru_api.data
{
    public class RuruDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid> {
        public RuruDbContext(DbContextOptions<RuruDbContext> options): base(options)
        {
        }
        public DbSet<User> Users {get; set;}
        public DbSet<Workout> Workout {get;set;}
        public DbSet<Exercise> Exercise {get;set;}
        public DbSet<WorkoutExercise> WorkoutExercise { get; set;}
        public DbSet<UserProfile> UserProfile { get; set; }
        public DbSet<UserConsent> UserConsent { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("UserProfile");
                entity.HasKey(p => p.UserId);

                // Personal data goes with the account (POPIA deletion requests).
                entity.HasOne(p => p.User)
                    .WithOne(u => u.Profile)
                    .HasForeignKey<UserProfile>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Single-choice answers are stored by name so the column stays readable.
                entity.Property(p => p.Sex).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.MainGoal).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.Pace).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.ActivityLevel).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.Experience).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.Sleep).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.TrainingLocation).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.Diet).HasConversion<string>().HasMaxLength(32);

                entity.Property(p => p.Height).HasPrecision(5, 1);
                entity.Property(p => p.CurrentWeight).HasPrecision(5, 1);
                entity.Property(p => p.GoalWeight).HasPrecision(5, 1);
            });

            modelBuilder.Entity<UserConsent>(entity =>
            {
                entity.ToTable("UserConsent");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Purpose).HasMaxLength(64);
                entity.Property(c => c.Version).HasMaxLength(32);
                entity.HasIndex(c => new { c.UserId, c.Purpose });

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Workout>(entity =>
            {
                entity.HasKey(w => w.Id);

                entity.HasOne(u => u.User)
                    .WithMany(u => u.Workouts)
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<WorkoutExercise>(entity =>
            {
                entity.HasOne(we => we.Workout)
                    .WithMany(w => w.WorkoutExercises)
                    .HasForeignKey(we => we.WorkoutId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(we => we.Exercise)
                    .WithMany(e => e.WorkoutExercise)
                    .HasForeignKey(we => we.ExerciseId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(u => u.Id);
            });

            modelBuilder.Entity<IdentityRole<Guid>>(entity =>
            {
                entity.ToTable("Role");
                entity.HasKey(r => r.Id);
                entity.HasData(
                    new IdentityRole<Guid>
                    {
                        Id = new Guid("38721ab2-ba90-4bba-aee3-53d9f87a7fa2"),
                        Name = "User",
                        NormalizedName = "USER",
                        ConcurrencyStamp = "a4828fab-220d-45ec-9310-4e214f8576e8"
                    },
                    new IdentityRole<Guid>
                    {
                        Id = new Guid("22a3a872-cfa3-4d31-84f1-ae5b2728a582"),
                        Name = "Admin",
                        NormalizedName = "ADMIN",
                        ConcurrencyStamp = "85b3fc77-c704-4905-9ebf-12b3d2f1c537"
                    }
                );
            });
            
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}