using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Helpers;
using ShoeStore.ScaffoldedModels;
using ShoeStore.Statics;

namespace ShoeStore;

public partial class OrdersWindow : Window
{
    public OrdersWindow()
    {
        InitializeComponent();
        ProfileName.Text = CurrentSession.CurrentUser?.FullName ?? "";
        ProfileRole.Text = CurrentSession.RoleName;
        OrderManagement.Visibility = CurrentSession.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
        LoadOrders();
    }

    private void LoadOrders()
    {
        var selectedId = (OrderList.SelectedItem as Order)?.OrderId;
        using var db = new ShoeStoreContext();
        var orders = db.Orders.AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.OrderStatus)
            .Include(order => order.PickupPoint).ThenInclude(point => point.Street).ThenInclude(street => street.City)
            .OrderByDescending(order => order.OrderId)
            .ToList();

        OrderList.ItemsSource = orders;
        OrderList.SelectedItem = orders.FirstOrDefault(order => order.OrderId == selectedId) ?? orders.FirstOrDefault();
    }

    private void OrderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrderLines is null)
        {
            return;
        }

        OrderLines.ItemsSource = null;
        OrderLinesTitle.Text = "Состав заказа";
        if (OrderList.SelectedItem is not Order order)
        {
            return;
        }

        try
        {
            using var db = new ShoeStoreContext();
            OrderLines.ItemsSource = db.OrderItems.AsNoTracking()
                .Include(item => item.Product).ThenInclude(product => product.ProductName)
                .Where(item => item.OrderId == order.OrderId)
                .OrderBy(item => item.OrderItemId)
                .ToList();
            OrderLinesTitle.Text = $"Состав заказа № {order.OrderId}";
        }
        catch (SqlException error)
        {
            MessageHelper.ShowDatabaseError(this, error);
        }
    }

    private void AddOrder_Click(object sender, RoutedEventArgs e) => EditOrder(null);

    private void EditOrder_Click(object sender, RoutedEventArgs e)
    {
        if (OrderList.SelectedItem is Order order)
        {
            EditOrder(order);
        }
        else
        {
            MessageHelper.ShowWarning(this, "Выбери заказ для редактирования.");
        }
    }

    private void EditOrder(Order? order)
    {
        if (!CurrentSession.IsAdministrator)
        {
            return;
        }

        try
        {
            var editor = new OrderWindow(order) { Owner = this };
            if (editor.ShowDialog() == true)
            {
                LoadOrders();
            }
        }
        catch (SqlException error)
        {
            MessageHelper.ShowDatabaseError(this, error);
        }
    }

    private void DeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (!CurrentSession.IsAdministrator)
        {
            return;
        }

        if (OrderList.SelectedItem is not Order order)
        {
            MessageHelper.ShowWarning(this, "Выбери заказ для удаления.");
            return;
        }

        var answer = MessageBox.Show(this, $"Удалить заказ № {order.OrderId} вместе с его позициями?",
            "Удаление заказа", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            using var db = new ShoeStoreContext();
            var existing = db.Orders.Find(order.OrderId);
            if (existing is not null)
            {
                db.Orders.Remove(existing);
                db.SaveChanges();
            }

            LoadOrders();
        }
        catch (Exception error) when (error is SqlException or DbUpdateException)
        {
            MessageHelper.ShowDatabaseError(this, error);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) => Close();
}

