using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ShoeStore.Helpers;
using ShoeStore.ScaffoldedModels;
using ShoeStore.Statics;

namespace ShoeStore;

public partial class ProductWindow : Window
{
    private readonly Product? product;
    private BitmapSource? selectedPhoto;

    public ProductWindow(Product? product)
    {
        InitializeComponent();
        this.product = product;

        Title = product is null ? "Добавление товара" : "Редактирование товара";
        FormTitle.Text = Title;
        using var db = new ShoeStoreContext();
        ProductNameBox.ItemsSource = db.ProductNames.OrderBy(item => item.Name).ToList();
        CategoryInput.ItemsSource = db.Categories.OrderBy(item => item.CategoryName).ToList();
        ManufacturerInput.ItemsSource = db.Manufacturers.OrderBy(item => item.ManufacturerName).ToList();
        SupplierInput.ItemsSource = db.Suppliers.OrderBy(item => item.SupplierName).ToList();
        ProductPhoto.Source = PhotoFiles.Load(product?.ImageFile);
        IdRow.Visibility = product is null ? Visibility.Collapsed : Visibility.Visible;
        DeleteButton.Visibility = IdRow.Visibility;

        if (product is null)
        {
            StockInput.Text = "0";
            DiscountInput.Text = "0";
            return;
        }

        ProductIdText.Text = product.ProductId.ToString();
        SkuInput.Text = product.Sku;
        ProductNameBox.Text = product.ProductName.Name;
        CategoryInput.SelectedValue = product.CategoryId;
        ManufacturerInput.SelectedValue = product.ManufacturerId;
        SupplierInput.SelectedValue = product.SupplierId;
        PriceInput.Text = product.Price.ToString("0.00");
        StockInput.Text = product.StockQuantity.ToString();
        DiscountInput.Text = product.DiscountPercent.ToString("0.##");
        DescriptionInput.Text = product.Description;
    }

    private void SelectPhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выбери фото товара",
            Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            selectedPhoto = PhotoFiles.Read(dialog.FileName);
            ProductPhoto.Source = selectedPhoto;
        }
        catch (Exception error) when (error is IOException or NotSupportedException or FormatException or UnauthorizedAccessException)
        {
            Trace.TraceError(error.ToString());
            MessageBox.Show(this, "Не удалось открыть изображение. Выбери исправный файл JPG, PNG или BMP.",
                "Ошибка фотографии", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!CurrentSession.IsAdministrator)
        {
            return;
        }

        var sku = SkuInput.Text.Trim();
        var name = ProductNameBox.Text.Trim();
        if (sku.Length == 0 || name.Length is 0 or > 120)
        {
            Warn("Введи артикул и наименование товара до 120 символов.");
            return;
        }

        if (CategoryInput.SelectedValue is not int category || ManufacturerInput.SelectedValue is not int manufacturer
            || SupplierInput.SelectedValue is not int supplier)
        {
            Warn("Выбери категорию, производителя и поставщика.");
            return;
        }

        if (!ReadAmount(PriceInput.Text, 9_999_999_999.99m, out var price))
        {
            Warn("Введи цену от 0 до 9 999 999 999,99. Допускается до двух знаков после запятой.");
            PriceInput.Focus();
            return;
        }

        if (!int.TryParse(StockInput.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var stock))
        {
            Warn("Введи неотрицательное целое количество товара.");
            StockInput.Focus();
            return;
        }

        if (!ReadAmount(DiscountInput.Text, 100, out var discount))
        {
            Warn("Введи скидку от 0 до 100 с точностью до двух знаков после запятой.");
            DiscountInput.Focus();
            return;
        }

        string? newFile = null;
        string? previousFile = null;
        try
        {
            if (selectedPhoto is not null)
            {
                newFile = PhotoFiles.Save(selectedPhoto);
            }

            using var db = new ShoeStoreContext();
            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
            var item = FindOrCreateProduct(db);
            var productName = db.ProductNames.SingleOrDefault(entry => entry.Name == name);
            if (productName is null)
            {
                productName = new ProductName { Name = name };
                db.ProductNames.Add(productName);
            }

            previousFile = item.ImageFile;
            item.Sku = sku;
            item.ProductName = productName;
            item.CategoryId = category;
            item.ManufacturerId = manufacturer;
            item.SupplierId = supplier;
            item.Price = price;
            item.DiscountPercent = discount;
            item.StockQuantity = stock;
            item.Description = DescriptionInput.Text.Trim();
            item.ImageFile = newFile ?? previousFile;
            db.SaveChanges();
            transaction.Commit();
        }
        catch (Exception error) when (error is SqlException or DbUpdateException)
        {
            RemovePhoto(newFile);
            Trace.TraceError(error.ToString());
            var message = MessageHelper.SqlErrorNumber(error) is 2601 or 2627
                ? "Этот артикул уже используется. Введи уникальный артикул."
                : "Не удалось сохранить товар. Обнови каталог и проверь подключение к SQL Server.";
            MessageBox.Show(this, message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        catch (Exception error) when (error is IOException or NotSupportedException or UnauthorizedAccessException)
        {
            RemovePhoto(newFile);
            Trace.TraceError(error.ToString());
            MessageBox.Show(this, "Не удалось сохранить фото. Проверь, доступна ли папка приложения для записи.",
                "Ошибка фотографии", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (newFile is not null)
        {
            RemoveUnusedPhoto(previousFile);
        }

        DialogResult = true;
    }

    private Product FindOrCreateProduct(ShoeStoreContext db)
    {
        if (product is not null)
        {
            return db.Products.Find(product.ProductId)
                ?? throw new DbUpdateConcurrencyException("Товар уже удалён.");
        }

        // По заданию ID нового товара равен текущему максимальному ID плюс один.
        var maxId = db.Products.Max(item => (int?)item.ProductId) ?? 0;
        var newProduct = new Product { ProductId = maxId + 1 };
        db.Products.Add(newProduct);
        return newProduct;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (!CurrentSession.IsAdministrator || product is null)
        {
            return;
        }

        var answer = MessageBox.Show(this, $"Удалить товар «{product.ProductName.Name}» ({product.Sku})?",
            "Удаление товара", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            using var db = new ShoeStoreContext();
            var item = db.Products.Find(product.ProductId)
                ?? throw new DbUpdateConcurrencyException("Товар уже удалён.");
            db.Products.Remove(item);
            db.SaveChanges();
            RemoveUnusedPhoto(item.ImageFile);
            DialogResult = true;
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 547 })
        {
            Warn("Этот товар есть в заказе. Удаление товара запрещено.");
        }
        catch (Exception error) when (error is SqlException or DbUpdateException)
        {
            Trace.TraceError(error.ToString());
            MessageBox.Show(this, "Не удалось удалить товар. Обнови каталог и повтори действие.",
                "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveUnusedPhoto(string? fileName)
    {
        if (fileName is null)
        {
            return;
        }

        try
        {
            using var db = new ShoeStoreContext();
            if (!db.Products.Any(item => item.ImageFile == fileName))
            {
                RemovePhoto(fileName);
            }
        }
        catch (SqlException error)
        {
            Trace.TraceError(error.ToString());
            Warn("Изменения в товаре сохранены. Проверь папку Photos: старое фото могло остаться.");
        }
    }

    private void RemovePhoto(string? fileName)
    {
        if (fileName is null)
        {
            return;
        }

        try
        {
            PhotoFiles.Delete(fileName);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Trace.TraceError(error.ToString());
            Warn("Не удалось удалить файл фотографии. Проверь права на папку Photos.");
        }
    }

    private static bool ReadAmount(string text, decimal limit, out decimal amount) =>
        decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out amount)
        && amount <= limit
        && decimal.Round(amount, 2) == amount;

    private void Warn(string text) => MessageHelper.ShowWarning(this, text);
}

