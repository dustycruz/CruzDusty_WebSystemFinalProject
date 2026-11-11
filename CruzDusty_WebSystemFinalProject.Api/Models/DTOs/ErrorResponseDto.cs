using System.Text.Json.Serialization;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    // Only present on validation failures: { "fieldName": ["message", ...] }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; set; }
}