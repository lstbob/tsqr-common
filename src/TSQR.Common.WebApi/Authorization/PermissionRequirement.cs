using Microsoft.AspNetCore.Authorization;

namespace TSQR.Common.WebApi.Authorization;

/// <summary>
/// Requires the authenticated principal to hold a named capability. The
/// capability can come from an explicit <c>permission</c> claim (new tokens) or
/// from the role claim fallback in <see cref="TSQR.Common.Authorization.RolePermissions"/>.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
