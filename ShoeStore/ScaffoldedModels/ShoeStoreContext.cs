using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShoeStore.ScaffoldedModels;

public partial class ShoeStoreContext : DbContext
{
    private const string DefaultConnection =
        @"Server=.\SQLEXPRESS;Database=ShoeStore2026;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;";

    public ShoeStoreContext()
    {
    }

    public ShoeStoreContext(DbContextOptions<ShoeStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Manufacturer> Manufacturers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<PickupPoint> PickupPoints { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductName> ProductNames { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Street> Streets { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = Environment.GetEnvironmentVariable("SHOE_STORE_CONNECTION") ?? DefaultConnection;
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B13E57FD5");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E04726DFBD").IsUnique();

            entity.Property(e => e.CategoryId).ValueGeneratedNever();
            entity.Property(e => e.CategoryName).HasMaxLength(120);
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityId).HasName("PK__Cities__F2D21B769261C064");

            entity.HasIndex(e => e.CityName, "UQ__Cities__886159E5D671BF60").IsUnique();

            entity.Property(e => e.CityId).ValueGeneratedNever();
            entity.Property(e => e.CityName).HasMaxLength(120);
        });

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasKey(e => e.ManufacturerId).HasName("PK__Manufact__357E5CC190D4257F");

            entity.HasIndex(e => e.ManufacturerName, "UQ__Manufact__3B9CDE2E4A34BF5B").IsUnique();

            entity.Property(e => e.ManufacturerId).ValueGeneratedNever();
            entity.Property(e => e.ManufacturerName).HasMaxLength(120);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF28DC499A");

            entity.Property(e => e.PickupCode).HasMaxLength(20);

            entity.HasOne(d => d.OrderStatus).WithMany(p => p.Orders)
                .HasForeignKey(d => d.OrderStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__OrderSta__534D60F1");

            entity.HasOne(d => d.PickupPoint).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PickupPointId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__PickupPo__52593CB8");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__UserId__5165187F");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED0681B4229932");

            entity.HasIndex(e => e.ProductId, "IX_OrderItems_ProductId");

            entity.HasIndex(e => new { e.OrderId, e.ProductId }, "UQ__OrderIte__08D097A244CC2515").IsUnique();

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__Order__5812160E");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Produ__59063A47");
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.OrderStatusId).HasName("PK__OrderSta__BC674CA1F260F588");

            entity.HasIndex(e => e.StatusName, "UQ__OrderSta__05E7698A953E2D49").IsUnique();

            entity.Property(e => e.OrderStatusId).ValueGeneratedNever();
            entity.Property(e => e.StatusName).HasMaxLength(60);
        });

        modelBuilder.Entity<PickupPoint>(entity =>
        {
            entity.HasKey(e => e.PickupPointId).HasName("PK__PickupPo__195D7EA07B2DC860");

            entity.HasIndex(e => new { e.StreetId, e.PostalCode, e.HouseNumber }, "UQ__PickupPo__398DE91BE19A8978").IsUnique();

            entity.Property(e => e.PickupPointId).ValueGeneratedNever();
            entity.Property(e => e.HouseNumber).HasMaxLength(30);
            entity.Property(e => e.PostalCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Street).WithMany(p => p.PickupPoints)
                .HasForeignKey(d => d.StreetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PickupPoi__Stree__4BAC3F29");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD4678EEBC");

            entity.HasIndex(e => e.Sku, "UQ__Products__CA1FD3C5722F4F7C").IsUnique();

            entity.Property(e => e.ProductId).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ImageFile).HasMaxLength(180);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Sku).HasMaxLength(32);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__Catego__3B75D760");

            entity.HasOne(d => d.Manufacturer).WithMany(p => p.Products)
                .HasForeignKey(d => d.ManufacturerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__Manufa__3C69FB99");

            entity.HasOne(d => d.ProductName).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductNameId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__Produc__3A81B327");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__Suppli__3D5E1FD2");
        });

        modelBuilder.Entity<ProductName>(entity =>
        {
            entity.HasKey(e => e.ProductNameId).HasName("PK__ProductN__343A705CFA9DD43E");

            entity.HasIndex(e => e.Name, "UQ__ProductN__DD5A978A30DA8811").IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .HasColumnName("ProductName");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AC640744E");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160E0C82780").IsUnique();

            entity.Property(e => e.RoleId).ValueGeneratedNever();
            entity.Property(e => e.RoleName).HasMaxLength(80);
        });

        modelBuilder.Entity<Street>(entity =>
        {
            entity.HasKey(e => e.StreetId).HasName("PK__Streets__6270EB3A1268F453");

            entity.HasIndex(e => new { e.CityId, e.StreetName }, "UQ__Streets__6024728D60EFCE82").IsUnique();

            entity.Property(e => e.StreetId).ValueGeneratedNever();
            entity.Property(e => e.StreetName).HasMaxLength(140);

            entity.HasOne(d => d.City).WithMany(p => p.Streets)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Streets__CityId__47DBAE45");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B4596B1B9E");

            entity.HasIndex(e => e.SupplierName, "UQ__Supplier__9C5DF66F2D536658").IsUnique();

            entity.Property(e => e.SupplierId).ValueGeneratedNever();
            entity.Property(e => e.SupplierName).HasMaxLength(120);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C08198C97");

            entity.HasIndex(e => e.Login, "UQ__Users__5E55825B909E458B").IsUnique();

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.FirstName).HasMaxLength(80);
            entity.Property(e => e.LastName).HasMaxLength(80);
            entity.Property(e => e.Login).HasMaxLength(120);
            entity.Property(e => e.Password).HasMaxLength(120);
            entity.Property(e => e.Patronymic).HasMaxLength(80);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__286302EC");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
