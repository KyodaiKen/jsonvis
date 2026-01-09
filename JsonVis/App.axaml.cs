using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using JsonVis.ViewModels;
using JsonVis.Views;

namespace JsonVis;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. Fenster erstellen
            var mainWindow = new MainWindow();

            // 2. ViewModel zuweisen (falls nicht schon im View passiert)
            mainWindow.DataContext = new MainWindowViewModel();

            desktop.MainWindow = mainWindow;

            // 3. Kommandozeilen-Argumente prüfen
            if (desktop.Args != null && desktop.Args.Length > 0)
            {
                // Die Datei laden, die als Argument übergeben wurde
                mainWindow.LoadFileFromArgs(desktop.Args[0]);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}