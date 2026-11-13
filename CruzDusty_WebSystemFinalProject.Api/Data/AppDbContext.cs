using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CruzDusty_WebSystemFinalProject.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Faculty> FacultyMembers => Set<Faculty>();

    public DbSet<StudentGrade> StudentGrades => Set<StudentGrade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configures the ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, ...).
        base.OnModelCreating(modelBuilder);

        // -----------------------------------------------------------------
        // Academic tables: created by Database/StudentGradeManagementDB.sql.
        // ExcludeFromMigrations: EF reads and writes them but never creates or alters them.
        // -----------------------------------------------------------------
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students", table => table.ExcludeFromMigrations());
            entity.HasKey(s => s.StudentId);

            entity.Property(s => s.StudentId).ValueGeneratedOnAdd();
            entity.Property(s => s.StudentNumber).HasMaxLength(30).IsRequired();
            entity.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(s => s.LastName).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(256).IsRequired();
            entity.Property(s => s.IsActive).IsRequired();

            entity.Ignore(s => s.FullName);

            entity.HasIndex(s => s.StudentNumber).IsUnique().HasDatabaseName("UQ_Students_StudentNumber");
            entity.HasIndex(s => s.Email).IsUnique().HasDatabaseName("UQ_Students_Email");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculty", table => table.ExcludeFromMigrations());
            entity.HasKey(f => f.FacultyId);

            entity.Property(f => f.FacultyId).ValueGeneratedOnAdd();
            entity.Property(f => f.FacultyNumber).HasMaxLength(30).IsRequired();
            entity.Property(f => f.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(f => f.LastName).HasMaxLength(100).IsRequired();
            entity.Property(f => f.Email).HasMaxLength(256).IsRequired();
            entity.Property(f => f.IsActive).IsRequired();

            entity.Ignore(f => f.FullName);

            entity.HasIndex(f => f.FacultyNumber).IsUnique().HasDatabaseName("UQ_Faculty_FacultyNumber");
            entity.HasIndex(f => f.Email).IsUnique().HasDatabaseName("UQ_Faculty_Email");
        });

        modelBuilder.Entity<StudentGrade>(entity =>
        {
            entity.ToTable("StudentGrades", table => table.ExcludeFromMigrations());
            entity.HasKey(g => g.GradeId);

            entity.Property(g => g.GradeId).ValueGeneratedOnAdd();
            entity.Property(g => g.SubjectCode).HasMaxLength(30).IsRequired();
            entity.Property(g => g.SubjectName).HasMaxLength(200).IsRequired();
            entity.Property(g => g.Grade).IsRequired();
            entity.Property(g => g.Remarks).HasMaxLength(30).IsRequired();
            entity.Property(g => g.Semester).HasMaxLength(50).IsRequired();
            entity.Property(g => g.SchoolYear).HasMaxLength(20).IsRequired();
            entity.Property(g => g.DatePosted).HasColumnType("datetime2(0)").IsRequired();
            entity.Property(g => g.DateModified).HasColumnType("datetime2(0)");

            entity.HasIndex(g => new { g.StudentId, g.SubjectCode, g.Semester, g.SchoolYear })
                .IsUnique()
                .HasDatabaseName("UQ_StudentGrades_StudentSubjectTerm");

            entity.HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StudentGrades_Students");

            entity.HasOne(g => g.Faculty)
                .WithMany(f => f.Grades)
                .HasForeignKey(g => g.FacultyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StudentGrades_Faculty");
        });

        // -----------------------------------------------------------------
        // Identity account -> academic record mapping (created by the EF migration).
        // -----------------------------------------------------------------
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers", table => table.HasCheckConstraint(
                "CK_AspNetUsers_SingleAcademicRecord",
                "[StudentId] IS NULL OR [FacultyId] IS NULL"));

            entity.HasOne(u => u.Student)
                .WithMany()
                .HasForeignKey(u => u.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AspNetUsers_Students");

            entity.HasOne(u => u.Faculty)
                .WithMany()
                .HasForeignKey(u => u.FacultyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AspNetUsers_Faculty");

            // One login per student record and one login per faculty record.
            entity.HasIndex(u => u.StudentId)
                .IsUnique()
                .HasFilter("[StudentId] IS NOT NULL")
                .HasDatabaseName("UX_AspNetUsers_StudentId");

            entity.HasIndex(u => u.FacultyId)
                .IsUnique()
                .HasFilter("[FacultyId] IS NOT NULL")
                .HasDatabaseName("UX_AspNetUsers_FacultyId");
        });

        // datetime2 has no time zone. Everything is stored as UTC, so mark values read back as UTC.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(utcConverter);
                }
            }
        }
    }
}