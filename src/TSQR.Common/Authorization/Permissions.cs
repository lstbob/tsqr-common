namespace TSQR.Common.Authorization;

/// <summary>
/// Canonical capability names used by the suite's policy-based authorization.
/// Permissions are intentionally finer-grained than the four JWT roles: a
/// controller/action declares the capability it needs, and
/// <see cref="RolePermissions"/> maps the current role claims to capabilities.
/// </summary>
public static class Permissions
{
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";

    public const string MembersRegister = "members.register";
    public const string MembersManage = "members.manage";

    public const string ReservationsSelfService = "reservations.self-service";
    public const string ReservationsConfirmPickup = "reservations.confirm-pickup";
    public const string ReservationsComplete = "reservations.complete";

    public const string ToolsManage = "tools.manage";
    public const string ToolsOperate = "tools.operate";
    public const string LoansManage = "loans.manage";

    public const string CommunitiesManage = "communities.manage";
    public const string GeographyManage = "geography.manage";
    public const string SupportManage = "support.manage";

    /// <summary>Every permission known to the current release.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        UsersManage,
        RolesManage,
        MembersRegister,
        MembersManage,
        ReservationsSelfService,
        ReservationsConfirmPickup,
        ReservationsComplete,
        ToolsManage,
        ToolsOperate,
        LoansManage,
        CommunitiesManage,
        GeographyManage,
        SupportManage,
    ];
}
