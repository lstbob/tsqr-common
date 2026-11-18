using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TSQR.Common.Authorization;

namespace TSQR.Common.WebApi.Authorization;

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> from explicit
/// <c>permission</c> claims, with a role-map fallback for access tokens issued
/// before permission claims existed.
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (HasPermissionClaim(context.User, requirement.Permission)
            || RoleMapGrants(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasPermissionClaim(ClaimsPrincipal user, string permission)
    {
        foreach (var claim in user.FindAll("permission").Concat(user.FindAll("permissions")))
        {
            foreach (var value in claim.Value.Split(
                [' ', ',', ';'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.Equals(value, permission, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static bool RoleMapGrants(ClaimsPrincipal user, string permission)
    {
        var roles = user.FindAll(ClaimTypes.Role).Concat(user.FindAll("role"));

        return roles.Any(role => RolePermissions.RoleHasPermission(role.Value, permission));
    }
}
