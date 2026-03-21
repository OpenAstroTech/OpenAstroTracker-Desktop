using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OATCommunications.Utilities;

namespace OATTest
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            Log.Init("OatTest");
            Log.EnableLogging();

            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                desktop.MainWindow = new MainWindow();
                desktop.Exit += (_, _) => Log.Quit();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
