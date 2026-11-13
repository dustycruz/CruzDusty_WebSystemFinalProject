using System.Security.Claims;

namespace CruzDusty_WebSystemFinalProject.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Reads an integer claim (studentId, facultyId) from the validated JWT.
    public static int? GetIntClaim(this ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        return int.TryParse(value, out var number) ? number : null;
    }
}