using System.Security.Claims;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Helper.AuthorizationHelper
{
    // Reads the caller's identity out of the JWT claims, so controllers don't
    // repeat int.Parse(User.FindFirstValue(...)) in every protected endpoint:
    //
    //     if (!User.CanAccessUser(id))
    //         return Forbidden("You can only view your own account.");
    //
    // Depends only on the claim types TokenService writes, so it moves to
    // another JWT project as-is.
    public static class ClaimsPrincipalExtensions
    {
        // Returns 0 when the claim is missing or unparseable, which never matches
        // a real user id, so callers don't need a null check of their own.
        public static int GetUserId(this ClaimsPrincipal user)
        {
            return int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }

        public static string? GetEmail(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Email);
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.IsInRole(nameof(UserRole.Admin));
        }

        // The Admin reaches any user record, everyone else only their own.
        public static bool CanAccessUser(this ClaimsPrincipal user, int userId)
        {
            return user.IsAdmin() || user.GetUserId() == userId;
        }
    }
}
