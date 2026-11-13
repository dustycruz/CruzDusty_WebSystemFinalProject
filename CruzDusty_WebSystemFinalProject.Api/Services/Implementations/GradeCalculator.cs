using CruzDusty_WebSystemFinalProject.Api.Exceptions;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

public class GradeCalculator : IGradeCalculator
{
    public const int MinimumGrade = 0;
    public const int MaximumGrade = 100;

    //  95-100 CMS | 90-94 SP | 85-89 SAP | 80-84 GP | 75-79 EP | 0-74 BEP/FAILED
    public string ComputeRemarks(int grade)
    {
        // Validated here as well, so the rule holds for every caller, not only HTTP requests.
        if (grade < MinimumGrade || grade > MaximumGrade)
        {
            throw ApiException.BadRequest("Grade must be a whole number from 0 to 100.");
        }

        if (grade >= 95)
        {
            return GradeRemarks.Cms;
        }

        if (grade >= 90)
        {
            return GradeRemarks.Sp;
        }

        if (grade >= 85)
        {
            return GradeRemarks.Sap;
        }

        if (grade >= 80)
        {
            return GradeRemarks.Gp;
        }

        if (grade >= 75)
        {
            return GradeRemarks.Ep;
        }

        return GradeRemarks.Failed;
    }
}