namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public sealed record GradeComputation(decimal? FinalGrade, string Remarks);

public interface IGradeCalculator
{
    // The ONLY place in the system where FinalGrade and Remarks are produced.
    GradeComputation Compute(decimal? prelim, decimal? midterm, decimal? finals);
}