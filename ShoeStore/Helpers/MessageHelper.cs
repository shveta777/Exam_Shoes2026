using System.Diagnostics;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace ShoeStore.Helpers;

internal static class MessageHelper
{
    public static void ShowWarning(Window owner, string message)
    {
        MessageBox.Show(owner, message, "Проверь данные", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public static void ShowError(Window owner, string message, string title)
    {
        MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowDatabaseError(Window owner, Exception error)
    {
        Trace.TraceError(error.ToString());
        ShowError(owner, "Проверь подключение к SQL Server и повтори действие.", "Ошибка базы данных");
    }

    public static int? SqlErrorNumber(Exception error)
    {
        var sqlError = error as SqlException ?? error.InnerException as SqlException;
        return sqlError?.Number;
    }
}

