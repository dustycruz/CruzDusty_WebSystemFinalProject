namespace CruzDusty_WebSystemFinalProject.Api.Models.Entities;

public class Subject
{
    public int SubjectId { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int Units { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<GradeRecord> GradeRecords { get; set; } = new List<GradeRecord>();
}