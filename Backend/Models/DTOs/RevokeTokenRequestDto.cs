using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTOs;

public class RevokeTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
