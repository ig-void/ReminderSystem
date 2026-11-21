using System.Security.Claims;
using Backend.Models;

namespace Backend.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string>? roles = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    int RefreshTokenExpirationDays { get; }
}
