using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TI5X.Avalonia.Services;
using TI5X.Avalonia.Views.Pages;
using TI5X.ViewModels;

namespace TI5X.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // The Android activity sets AppServices.FileService/Exit before base.OnCreate;
        // the view model is created here in both heads exactly once.
        if (AppServices.Main == null)
        {
            AppServices.Main = new MainViewModel(new AvaloniaUiDispatcher());
            EmbeddedAssets.LoadMasterLibrary(AppServices.Main);
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AppServices.FileService ??= new StorageFileService(() => desktop.MainWindow);
            AppServices.Exit = () => desktop.MainWindow?.Close();

            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // Android (Avalonia.Android): the Android lifetime also implements
            // IActivityApplicationLifetime, whose MainViewFactory is the supported way
            // to (re)create the root view for every activity instance.
            if (ApplicationLifetime is IActivityApplicationLifetime activityLifetime)
            {
                activityLifetime.MainViewFactory = CreateMainView;
            }
            else
            {
                singleView.MainView = CreateMainView();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static PageNavigationHost CreateMainView()
    {
        // PageNavigationHost is required on Android to route system back presses into
        // the navigation stack and to apply safe-area insets (edge-to-edge).
        return new PageNavigationHost
        {
            Page = new NavigationPage
            {
                Content = new CalculatorPage { DataContext = AppServices.Main }
            }
        };
    }
}