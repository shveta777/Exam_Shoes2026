using ShoeStore.ScaffoldedModels;

namespace ShoeStore.Statics;

internal static class CurrentSession
{
    private const int AdministratorRoleId = 1;
    private const int ManagerRoleId = 2;

    public static User? CurrentUser { get; set; }

    public static bool IsAdministrator => IsAdministratorFor(CurrentUser);

    public static bool CanViewOrders => CanViewOrdersFor(CurrentUser);

    public static string RoleName => RoleNameFor(CurrentUser);

    public static bool IsAdministratorFor(User? user) => user?.RoleId == AdministratorRoleId;

    public static bool CanViewOrdersFor(User? user) => user?.RoleId is AdministratorRoleId or ManagerRoleId;

    public static string RoleNameFor(User? user) => user?.RoleId switch
    {
        AdministratorRoleId => "Администратор",
        ManagerRoleId => "Менеджер",
        _ => user is null ? "Гость" : "Клиент"
    };
}

