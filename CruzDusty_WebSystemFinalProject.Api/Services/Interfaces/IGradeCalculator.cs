namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public interface IGradeCalculator
{
    // The ONLY place in the system where Remarks are produced.
    string ComputeRemarks(int grade);
}