using System.ComponentModel.DataAnnotations;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, ErrorMessage = "Username must be at most 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password must be at most 100 characters.")]
    public string Password { get; set; } = string.Empty;
}