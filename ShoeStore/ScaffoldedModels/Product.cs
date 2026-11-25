using System;
using System.Collections.Generic;

namespace ShoeStore.ScaffoldedModels;

public partial class Product
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public int ProductNameId { get; set; }

    public int CategoryId { get; set; }

    public int ManufacturerId { get; set; }

    public int SupplierId { get; set; }

    public decimal Price { get; set; }

    public decimal DiscountPercent { get; set; }

    public int StockQuantity { get; set; }

    public string Description { get; set; } = null!;

    public string? ImageFile { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Manufacturer Manufacturer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ProductName ProductName { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;

    public string Title => $"{Category.CategoryName} | {ProductName.Name}";
    public string SkuAndName => $"{Sku} | {ProductName.Name}";
    public decimal FinalPrice => Math.Round(Price * (1 - DiscountPercent / 100), 2, MidpointRounding.AwayFromZero);
    public string PriceText => $"{Price:N2} ₽";
    public string FinalPriceText => $"{FinalPrice:N2} ₽";
    public string DiscountText => $"{DiscountPercent:0.##}%";
    public bool HasDiscount => DiscountPercent > 0;
    public bool HasLargeDiscount => DiscountPercent > 15;
    public bool IsOutOfStock => StockQuantity == 0;
}
