using System;
using System.Collections.Generic;

namespace ShoeStore.ScaffoldedModels;

public partial class PickupPoint
{
    public int PickupPointId { get; set; }

    public int StreetId { get; set; }

    public string PostalCode { get; set; } = null!;

    public string? HouseNumber { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Street Street { get; set; } = null!;

    public string FullAddress
    {
        get
        {
            var address = $"{PostalCode}, г. {Street.City.CityName}, ул. {Street.StreetName}";
            return string.IsNullOrEmpty(HouseNumber) ? address : $"{address}, {HouseNumber}";
        }
    }
}
