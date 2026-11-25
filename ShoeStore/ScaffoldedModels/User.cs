using System;
using System.Collections.Generic;

namespace ShoeStore.ScaffoldedModels;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string Patronymic { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;

    public string FullName => $"{LastName} {FirstName} {Patronymic}";

    public string CustomerLabel => $"{FullName} (ID {UserId})";
}
