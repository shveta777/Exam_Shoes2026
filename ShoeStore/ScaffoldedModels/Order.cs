using System;
using System.Collections.Generic;

namespace ShoeStore.ScaffoldedModels;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int PickupPointId { get; set; }

    public int OrderStatusId { get; set; }

    public DateOnly? OrderDate { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public string PickupCode { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual OrderStatus OrderStatus { get; set; } = null!;

    public virtual PickupPoint PickupPoint { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
