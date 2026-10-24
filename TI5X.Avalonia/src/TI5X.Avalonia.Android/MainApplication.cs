using Android.App;
using Android.Content;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Fonts.Inter;
using TI5X.Avalonia.Services;

namespace TI5X.Avalonia.Android;

// Android application entry point: initializes Avalonia (AppBuilder) before any
// activity starts. The android:app class must derive from Android.App.Application,
// which is what AvaloniaAndroidApplication<App> provides.
[Application]
public class MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
    : AvaloniaAndroidApplication<App>(javaReference, transfer)
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();
    }
}