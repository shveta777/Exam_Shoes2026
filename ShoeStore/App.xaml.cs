using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace ShoeStore;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("ru-RU")));
        base.OnStartup(e);
        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}

