using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CruzDusty_WebSystemFinalProject.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.UserId).ValueGeneratedOnAdd();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.Property(u => u.IsActive).IsRequired();
            entity.Property(u => u.CreatedAt).IsRequired();
            entity.Property(u => u.UpdatedAt);

            entity.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Username");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(s => s.StudentId);

            entity.Property(s => s.StudentId).ValueGeneratedOnAdd();
            entity.Property(s => s.StudentNumber).HasMaxLength(20).IsRequired();
            entity.Property(s => s.FirstName).HasMaxLength(50).IsRequired();
            entity.Property(s => s.MiddleName).HasMaxLength(50);
            entity.Property(s => s.LastName).HasMaxLength(50).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(150);
            entity.Property(s => s.Course).HasMaxLength(100).IsRequired();
            entity.Property(s => s.YearLevel).IsRequired();
            entity.Property(s => s.CreatedAt).IsRequired();
            entity.Property(s => s.UpdatedAt);

            entity.Ignore(s => s.FullName);
            entity.Ignore(s => s.ListName);

            entity.HasIndex(s => s.StudentNumber)
                .IsUnique()
                .HasDatabaseName("UQ_Students_StudentNumber");

            entity.HasIndex(s => s.UserId)
                .IsUnique()
                .HasDatabaseName("UX_Students_UserId")
                .HasFilter("[UserId] IS NOT NULL");

            entity.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Students_Users");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subjects");
            entity.HasKey(s => s.SubjectId);

            entity.Property(s => s.SubjectId).ValueGeneratedOnAdd();
            entity.Property(s => s.SubjectCode).HasMaxLength(20).IsRequired();
            entity.Property(s => s.SubjectName).HasMaxLength(150).IsRequired();
            entity.Property(s => s.Units).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.CreatedAt).IsRequired();
            entity.Property(s => s.UpdatedAt);

            entity.HasIndex(s => s.SubjectCode)
                .IsUnique()
                .HasDatabaseName("UQ_Subjects_SubjectCode");
        });

        modelBuilder.Entity<GradeRecord>(entity =>
        {
            entity.ToTable("GradeRecords");
            entity.HasKey(g => g.GradeRecordId);

            entity.Property(g => g.GradeRecordId).ValueGeneratedOnAdd();
            entity.Property(g => g.SchoolYear).HasMaxLength(9).IsRequired();
            entity.Property(g => g.Semester).HasMaxLength(20).IsRequired();
            entity.Property(g => g.Prelim).HasColumnType("decimal(5,2)");
            entity.Property(g => g.Midterm).HasColumnType("decimal(5,2)");
            entity.Property(g => g.Finals).HasColumnType("decimal(5,2)");
            entity.Property(g => g.FinalGrade).HasColumnType("decimal(5,2)");
            entity.Property(g => g.Remarks).HasMaxLength(20).IsRequired();
            entity.Property(g => g.CreatedAt).IsRequired();
            entity.Property(g => g.UpdatedAt);

            entity.HasIndex(g => new { g.StudentId, g.SubjectId, g.SchoolYear, g.Semester })
                .IsUnique()
                .HasDatabaseName("UQ_GradeRecords_Student_Subject_Term");

            entity.HasIndex(g => g.SubjectId)
                .HasDatabaseName("IX_GradeRecords_SubjectId");

            entity.HasOne(g => g.Student)
                .WithMany(s => s.GradeRecords)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_GradeRecords_Students");

            entity.HasOne(g => g.Subject)
                .WithMany(s => s.GradeRecords)
                .HasForeignKey(g => g.SubjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GradeRecords_Subjects");
        });

        // SQL Server datetime2 has no time zone. Everything is stored as UTC,
        // so mark every DateTime read from the database as UTC.
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