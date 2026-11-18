using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TSQR.Common.Authorization;
using TSQR.Common.WebApi.Authorization;

namespace TSQR.Common.WebApi.Tests;

public class PermissionAuthorizationTests
{
    [Theory]
    [InlineData("Admin", Permissions.UsersManage)]
    [InlineData("Admin", Permissions.GeographyManage)]
    [InlineData("LocationCoordinator", Permissions.MembersRegister)]
    [InlineData("LocationCoordinator", Permissions.CommunitiesManage)]
    [InlineData("Repairman", Permissions.ToolsOperate)]
    [InlineData("Regular", Permissions.ReservationsSelfService)]
    public async Task RoleFallback_GrantsKnownPermission(string role, string permission)
    {
        var context = CreateContext(role, permission);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData("Regular", Permissions.ToolsManage)]
    [InlineData("Repairman", Permissions.MembersManage)]
    [InlineData("LocationCoordinator", Permissions.UsersManage)]
    public async Task RoleFallback_RejectsUnknownPermission(string role, string permission)
    {
        var context = CreateContext(role, permission);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task ExplicitPermissionClaim_OverridesRoleMap()
    {
        var context = CreateContext(
            role: "Regular",
            permission: Permissions.ToolsManage,
            permissionClaims: [Permissions.ToolsManage]);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UnauthenticatedPrincipal_IsRejected()
    {
        var requirement = new PermissionRequirement(Permissions.UsersManage);
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public void AdminRole_HasEveryDefinedPermission()
    {
        var permissions = RolePermissions.ForRole("Admin");

        Assert.All(Permissions.All, permission =>
            Assert.Contains(permission, permissions));
    }

    private static AuthorizationHandlerContext CreateContext(
        string role,
        string permission,
        params string[] permissionClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
        };
        claims.AddRange(permissionClaims.Select(value =>
            new Claim("permission", value)));

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var requirement = new PermissionRequirement(permission);

        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }
}
