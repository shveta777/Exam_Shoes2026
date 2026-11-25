using System.Windows;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Helpers;
using ShoeStore.ScaffoldedModels;
using ShoeStore.Statics;

namespace ShoeStore;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoginInput.Focus();
    }

    private void SignIn_Click(object sender, RoutedEventArgs e)
    {
        var login = LoginInput.Text.Trim();
        var password = PasswordInput.Password;

        if (login.Length == 0 || password.Length == 0)
        {
            ShowLoginError("Введи логин и пароль.");
            return;
        }

        try
        {
            using var db = new ShoeStoreContext();
            var user = db.Users.AsNoTracking().SingleOrDefault(user => user.Login == login);

            if (user is null || !string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                ShowLoginError("Неверный логин или пароль. Проверь данные и попробуй ещё раз.");
                PasswordInput.SelectAll();
                PasswordInput.Focus();
                return;
            }

            OpenCatalog(user);
        }
        catch (SqlException error)
        {
            MessageHelper.ShowDatabaseError(this, error);
        }
    }

    private void Guest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OpenCatalog(null);
        }
        catch (SqlException error)
        {
            MessageHelper.ShowDatabaseError(this, error);
        }
    }

    private void OpenCatalog(User? user)
    {
        var catalog = new ShoesWindow(user);
        CurrentSession.CurrentUser = user;
        catalog.Show();
        Application.Current.MainWindow = catalog;
        Close();
    }

    private void ShowLoginError(string message)
    {
        LoginError.Text = message;
        LoginError.Visibility = Visibility.Visible;
    }
}

