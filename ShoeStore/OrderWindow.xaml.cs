using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Helpers;
using ShoeStore.ScaffoldedModels;
using ShoeStore.Statics;

namespace ShoeStore;

public partial class OrderWindow : Window
{
    private readonly Order? order;
    private readonly ObservableCollection<OrderItem> lines = [];

    public OrderWindow(Order? order)
    {
        InitializeComponent();
        this.order = order;

        Title = order is null ? "Добавление заказа" : $"Редактирование заказа № {order.OrderId}";
        FormTitle.Text = Title;
        using var db = new ShoeStoreContext();
        CustomerInput.ItemsSource = db.Users.AsNoTracking()
            .OrderBy(user => user.LastName).ThenBy(user => user.FirstName).ThenBy(user => user.UserId)
            .ToList();
        PickupPointInput.ItemsSource = db.PickupPoints.AsNoTracking()
            .Include(point => point.Street).ThenInclude(street => street.City)
            .OrderBy(point => point.PickupPointId)
            .ToList();
        var statuses = db.OrderStatuses.OrderBy(status => status.OrderStatusId).ToList();
        StatusInput.ItemsSource = statuses;
        ProductInput.ItemsSource = db.Products.AsNoTracking()
            .Include(product => product.ProductName)
            .OrderBy(product => product.ProductId)
            .ToList();

        LinesGrid.ItemsSource = lines;
        if (order is null)
        {
            OrderDateInput.SelectedDate = DateTime.Today;
            DeliveryDateInput.SelectedDate = DateTime.Today;
            StatusInput.SelectedItem = statuses.FirstOrDefault(status => status.StatusName == "Новый");
            return;
        }

        CustomerInput.SelectedValue = order.UserId;
        PickupPointInput.SelectedValue = order.PickupPointId;
        StatusInput.SelectedValue = order.OrderStatusId;
        OrderDateInput.SelectedDate = order.OrderDate?.ToDateTime(TimeOnly.MinValue);
        DeliveryDateInput.SelectedDate = order.DeliveryDate.ToDateTime(TimeOnly.MinValue);
        CodeInput.Text = order.PickupCode;

        var orderItems = db.OrderItems.AsNoTracking()
            .Include(item => item.Product).ThenInclude(product => product.ProductName)
            .Where(item => item.OrderId == order.OrderId)
            .OrderBy(item => item.OrderItemId)
            .ToList();
        foreach (var item in orderItems)
        {
            lines.Add(item);
        }
    }

    private void AddLine_Click(object sender, RoutedEventArgs e)
    {
        if (ProductInput.SelectedItem is not Product product
            || !int.TryParse(QuantityInput.Text.Trim(), out var quantity) || quantity <= 0)
        {
            Warn("Выбери товар и укажи положительное целое количество.");
            return;
        }

        var existing = lines.FirstOrDefault(line => line.ProductId == product.ProductId);
        var line = new OrderItem { ProductId = product.ProductId, Product = product, Quantity = quantity };
        if (existing is null)
        {
            lines.Add(line);
        }
        else
        {
            lines[lines.IndexOf(existing)] = line;
        }

        LinesGrid.SelectedItem = line;
    }

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (LinesGrid.SelectedItem is OrderItem line)
        {
            lines.Remove(line);
        }
        else
        {
            Warn("Выбери позицию в составе заказа.");
        }
    }

    private void LinesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LinesGrid.SelectedItem is OrderItem line)
        {
            ProductInput.SelectedValue = line.ProductId;
            QuantityInput.Text = line.Quantity.ToString();
        }
    }

    private void OrderDateInput_Error(object sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        Warn("Введи корректную дату в формате ДД.ММ.ГГГГ.");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!CurrentSession.IsAdministrator)
        {
            return;
        }

        var code = CodeInput.Text.Trim();
        if (CustomerInput.SelectedValue is not int customer || PickupPointInput.SelectedValue is not int pickupPoint
            || StatusInput.SelectedValue is not int status || code.Length == 0)
        {
            Warn("Выбери покупателя, пункт выдачи, статус и введи код получения.");
            return;
        }

        if (OrderDateInput.SelectedDate is not DateTime orderDate
            || DeliveryDateInput.SelectedDate is not DateTime delivery || delivery < orderDate)
        {
            Warn("Укажи дату заказа и дату доставки. Доставка не может быть раньше даты заказа.");
            return;
        }

        if (lines.Count == 0)
        {
            Warn("Добавь хотя бы один товар в заказ.");
            return;
        }

        try
        {
            using var db = new ShoeStoreContext();
            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
            var item = FindOrCreateOrder(db);
            item.UserId = customer;
            item.PickupPointId = pickupPoint;
            item.OrderStatusId = status;
            item.OrderDate = DateOnly.FromDateTime(orderDate);
            item.DeliveryDate = DateOnly.FromDateTime(delivery);
            item.PickupCode = code;
            SaveOrderItems(db, item);
            db.SaveChanges();
            transaction.Commit();
            DialogResult = true;
        }
        catch (Exception error) when (error is SqlException or DbUpdateException)
        {
            Trace.TraceError(error.ToString());
            var message = MessageHelper.SqlErrorNumber(error) == 547
                ? "Товар или другие данные заказа изменены. Открой форму заново и проверь позиции."
                : "Не удалось сохранить заказ. Проверь подключение к SQL Server и повтори действие.";
            MessageBox.Show(this, message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Order FindOrCreateOrder(ShoeStoreContext db)
    {
        if (order is not null)
        {
            return db.Orders.Include(item => item.OrderItems)
                .SingleOrDefault(item => item.OrderId == order.OrderId)
                ?? throw new DbUpdateConcurrencyException("Заказ уже удалён.");
        }

        var newOrder = new Order();
        db.Orders.Add(newOrder);
        return newOrder;
    }

    private void SaveOrderItems(ShoeStoreContext db, Order savedOrder)
    {
        var changes = lines.ToDictionary(line => line.ProductId);
        foreach (var item in savedOrder.OrderItems.ToList())
        {
            if (changes.Remove(item.ProductId, out var requested))
            {
                item.Quantity = requested.Quantity;
            }
            else
            {
                db.OrderItems.Remove(item);
            }
        }

        foreach (var line in changes.Values)
        {
            savedOrder.OrderItems.Add(new OrderItem { ProductId = line.ProductId, Quantity = line.Quantity });
        }
    }

    private void Warn(string text) => MessageHelper.ShowWarning(this, text);
}

