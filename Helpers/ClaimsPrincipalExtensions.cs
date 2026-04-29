using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace VehiclePartsBackend.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var idText =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            user.FindFirstValue("sub");

        if (int.TryParse(idText, out var id))
        {
            return id;
        }

        return null;
    }
}
