using Microsoft.AspNetCore.Identity;

namespace CruzDusty_WebSystemFinalProject.Api.Models.Entities;

// The ASP.NET Core Identity login account, extended with the link to the academic record.
// A Student login has StudentId set; a Faculty login has FacultyId set; never both.
public class ApplicationUser : IdentityUser
{
    public int? StudentId { get; set; }

    public Student? Student { get; set; }

    public int? FacultyId { get; set; }

    public Faculty? Faculty { get; set; }
}