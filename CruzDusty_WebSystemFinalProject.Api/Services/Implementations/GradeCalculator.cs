using CruzDusty_WebSystemFinalProject.Api.Exceptions;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

public class GradeCalculator : IGradeCalculator
{
    public const decimal PrelimWeight = 0.30m;
    public const decimal MidtermWeight = 0.30m;
    public const decimal FinalsWeight = 0.40m;
    public const decimal PassingGrade = 75m;
    public const decimal MinimumScore = 0m;
    public const decimal MaximumScore = 100m;

    public GradeComputation Compute(decimal? prelim, decimal? midterm, decimal? finals)
    {
        ValidateScore(prelim, "Prelim");
        ValidateScore(midterm, "Midterm");
        ValidateScore(finals, "Finals");

        // Any missing score: the grade is Incomplete and FinalGrade stays null.
        if (prelim is null || midterm is null || finals is null)
        {
            return new GradeComputation(null, GradeRemarks.Incomplete);
        }

        var weighted =
            (prelim.Value * PrelimWeight) +
            (midterm.Value * MidtermWeight) +
            (finals.Value * FinalsWeight);

        var finalGrade = Math.Round(weighted, 2, MidpointRounding.AwayFromZero);

        var remarks = finalGrade >= PassingGrade
            ? GradeRemarks.Passed
            : GradeRemarks.Failed;

        return new GradeComputation(finalGrade, remarks);
    }

    private static void ValidateScore(decimal? score, string name)
    {
        if (score is null)
        {
            return;
        }

        if (score.Value < MinimumScore || score.Value > MaximumScore)
        {
            throw ApiException.BadRequest($"{name} must be between 0 and 100.");
        }
    }
}