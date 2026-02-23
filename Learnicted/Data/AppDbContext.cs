using Microsoft.EntityFrameworkCore;
using Learnicted.Models;

namespace Learnicted.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProgress> UserProgresses => Set<UserProgress>();
    public DbSet<UserUnit> UserUnits => Set<UserUnit>();
    public DbSet<UserCourse> UserCourses => Set<UserCourse>();
    public DbSet<UserRemediation> UserRemediations => Set<UserRemediation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =============================
        // TABLO İSİMLERİ
        // =============================
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<UserProgress>().ToTable("UserProgresses");
        modelBuilder.Entity<UserUnit>().ToTable("UserUnits");
        modelBuilder.Entity<UserCourse>().ToTable("UserCourses");
        modelBuilder.Entity<UserRemediation>().ToTable("UserRemediations");

        // =============================
        // USER - USERPROGRESS (1-1)
        // =============================
        modelBuilder.Entity<User>()
            .HasOne(u => u.Progress)
            .WithOne(p => p.User)
            .HasForeignKey<UserProgress>(p => p.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProgress>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        // =============================
        // USER - USERCOURSE (1-N)
        // =============================
        modelBuilder.Entity<UserCourse>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================
        // USER - USERUNIT (1-N)
        // =============================
        modelBuilder.Entity<UserUnit>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(uu => uu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================
        // USER - USERREMEDIATION (1-N)
        // =============================
        modelBuilder.Entity<UserRemediation>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
