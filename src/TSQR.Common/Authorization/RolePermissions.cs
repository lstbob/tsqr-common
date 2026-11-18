namespace TSQR.Common.Authorization;

/// <summary>
/// Default role-to-permission mapping. This is the fallback used when a JWT was
/// issued before permission claims were added; new tokens carry explicit
/// <c>permission</c> claims, so the mapping can be replaced by persisted role
/// permissions without changing controller attributes.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Map =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = new HashSet<string>(Permissions.All, StringComparer.OrdinalIgnoreCase),
            ["LocationCoordinator"] = Set(
                Permissions.MembersRegister,
                Permissions.ReservationsSelfService,
                Permissions.ReservationsConfirmPickup,
                Permissions.ReservationsComplete,
                Permissions.ToolsManage,
                Permissions.ToolsOperate,
                Permissions.CommunitiesManage,
                Permissions.SupportManage),
            ["Repairman"] = Set(
                Permissions.ReservationsSelfService,
                Permissions.ReservationsConfirmPickup,
                Permissions.ToolsOperate),
            ["Regular"] = Set(Permissions.ReservationsSelfService),
        };

    public static IReadOnlySet<string> ForRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return EmptySet;

        return Map.TryGetValue(role.Trim(), out var permissions)
            ? permissions
            : EmptySet;
    }

    public static bool RoleHasPermission(string? role, string permission) =>
        ForRole(role).Contains(permission);

    private static IReadOnlySet<string> Set(params string[] permissions) =>
        new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlySet<string> EmptySet =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
