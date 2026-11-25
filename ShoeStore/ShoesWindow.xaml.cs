using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Helpers;
using ShoeStore.ScaffoldedModels;
using ShoeStore.Statics;

namespace ShoeStore;

public partial class ShoesWindow : Window
{
    private List<Product> products = [];

    public ShoesWindow(User? user)
    {
        InitializeComponent();

        ProfileName.Text = user?.FullName ?? "";
        ProfileRole.Text = CurrentSession.RoleNameFor(user);
        var canViewOrders = CurrentSession.CanViewOrdersFor(user);
        CatalogTools.Visibility = canViewOrders ? Visibility.Visible : Visibility.Collapsed;
        OrdersButton.Visibility = canViewOrders ? Visibility.Visible : Visibility.Collapsed;
        AddProductButton.Visibility = CurrentSession.IsAdministratorFor(user)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (canViewOrders)
        {
            using var db = new ShoeStoreContext();
            var suppliers = db.Suppliers.AsNoTracking().OrderBy(supplier => supplier.SupplierName).ToList();
            suppliers.Insert(0, new Supplier { SupplierName = "Все поставщики" });
            SupplierFilter.ItemsSource = suppliers;
            SupplierFilter.SelectedIndex = 0;
            StockSort.SelectedIndex = 0;
        }

        LoadShoes();
    }

    private void LoadShoes()
    {
        using var db = new ShoeStoreContext();
        products = db.Products
            .AsNoTracking()
            .Include(product => product.ProductName)
            .Include(product => product.Category)
            .Include(product => product.Manufacturer)
            .Include(product => product.Supplier)
            .OrderBy(product => product.ProductId)
            .ToList();

        UpdateCards();
    }

    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e) => UpdateCards();


    private void CatalogChoice_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateCards();

        if (sender == StockSort && CatalogList != null && CatalogList.Items.Count > 0)
        {
            CatalogList.ScrollIntoView(CatalogList.Items[0]);
        }
    }

    private void UpdateCards()
    {
        if (CatalogList is null)
        {
            return;
        }

        IEnumerable<Product> result = products;
        if (CatalogTools.Visibility == Visibility.Visible)
        {
            if (SupplierFilter.SelectedValue is int supplierId && supplierId > 0)
            {
                result = result.Where(product => product.SupplierId == supplierId);
            }

            var terms = SearchInput.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (terms.Length > 0)
            {
                result = result.Where(product => Matches(product, terms));
            }

            result = StockSort.SelectedIndex switch
            {
                1 => result.OrderBy(product => product.StockQuantity).ThenBy(product => product.ProductId),
                2 => result.OrderByDescending(product => product.StockQuantity).ThenBy(product => product.ProductId),
                _ => result
            };
        }

        var visibleProducts = result.ToList();
        CatalogList.ItemsSource = visibleProducts;
        CatalogEmpty.Visibility = visibleProducts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CatalogCount.Text = $"Товаров: {visibleProducts.Count}";
    }

    private static bool Matches(Product product, string[] terms)
    {
        var text = string.Join(" ", product.Sku, product.ProductName.Name, product.Category.CategoryName,
            product.Description, product.Manufacturer.ManufacturerName, product.Supplier.SupplierName, product.ImageFile);
        return terms.All(term => text.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    private void CatalogItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: Product product })
        {
            EditProduct(product.ProductId);
        }
    }

    private void CatalogList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && CurrentSession.IsAdministrator && CatalogList.SelectedItem is Product product)
        {
            e.Handled = true;
            EditProduct(product.ProductId);
        }
    }

    private void AddProduct_Click(object sender, RoutedEventArgs e) => EditProduct(null);

    private void EditProduct(int? productId)
    {
        if (!CurrentSession.IsAdministrator)
        {
            return;
        }

        try
        {
            Product? product = null;
            if (productId is int id)
            {
                using var db = new ShoeStoreContext();
                product = db.Products.AsNoTracking().Include(item => item.ProductName)
                    .SingleOrDefault(item => item.ProductId == id);
                if (product is null)
                {
                    LoadShoes();
                    MessageHelper.ShowWarning(this, "Товар уже удалён. Каталог обновлён.");
                    return;
                }
            }

            var editor = new ProductWindow(product) { Owner = this };
            if (editor.ShowDialog() == true)
            {
                LoadShoes();
            }
        }
        catch (SqlException error)
        {
            MessageHelper.ShowDatabaseError(this, error);
        }
    }

    private void Orders_Click(object sender, RoutedEventArgs e)
    {
        if (!CurrentSession.CanViewOrders)
        {
            return;
        }

        try
        {
            var orders = new OrdersWindow { Owner = this };
            orders.ShowDialog();
            LoadShoes();
        }
        catch (SqlException error)
        {
            MessageHelper.ShowDatabaseError(this, error);
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        CurrentSession.CurrentUser = null;
        var login = new MainWindow();
        login.Show();
        Application.Current.MainWindow = login;
        Close();
    }
}

