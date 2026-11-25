using System;
using System.Collections.Generic;

namespace ShoeStore.ScaffoldedModels;

public partial class City
{
    public int CityId { get; set; }

    public string CityName { get; set; } = null!;

    public virtual ICollection<Street> Streets { get; set; } = new List<Street>();
}
