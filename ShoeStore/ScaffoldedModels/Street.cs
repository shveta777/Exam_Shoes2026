using System;
using System.Collections.Generic;

namespace ShoeStore.ScaffoldedModels;

public partial class Street
{
    public int StreetId { get; set; }

    public int CityId { get; set; }

    public string StreetName { get; set; } = null!;

    public virtual City City { get; set; } = null!;

    public virtual ICollection<PickupPoint> PickupPoints { get; set; } = new List<PickupPoint>();
}
