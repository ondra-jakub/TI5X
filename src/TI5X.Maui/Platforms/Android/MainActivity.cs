using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using TI5X.Avalonia.Core;

namespace TI5X.Maui;

[Activity(Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AndroidDocumentSaver.HandleResult(requestCode, resultCode, data);
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        int? tiKey = keyCode switch
        {
            Keycode.Num0 or Keycode.Numpad0 => KeyCodes.KEY_0,
            Keycode.Num1 or Keycode.Numpad1 => KeyCodes.KEY_1,
            Keycode.Num2 or Keycode.Numpad2 => KeyCodes.KEY_2,
            Keycode.Num3 or Keycode.Numpad3 => KeyCodes.KEY_3,
            Keycode.Num4 or Keycode.Numpad4 => KeyCodes.KEY_4,
            Keycode.Num5 or Keycode.Numpad5 => KeyCodes.KEY_5,
            Keycode.Num6 or Keycode.Numpad6 => KeyCodes.KEY_6,
            Keycode.Num7 or Keycode.Numpad7 => KeyCodes.KEY_7,
            Keycode.Num8 or Keycode.Numpad8 => KeyCodes.KEY_8,
            Keycode.Num9 or Keycode.Numpad9 => KeyCodes.KEY_9,
            Keycode.Period or Keycode.NumpadDot => KeyCodes.KEY_DOT,
            Keycode.Plus or Keycode.NumpadAdd => KeyCodes.KEY_ADD,
            Keycode.Minus or Keycode.NumpadSubtract => KeyCodes.KEY_SUB,
            Keycode.Star or Keycode.NumpadMultiply => KeyCodes.KEY_MUL,
            Keycode.Slash or Keycode.NumpadDivide => KeyCodes.KEY_DIV,
            Keycode.Enter or Keycode.NumpadEnter => KeyCodes.KEY_EQUALS,
            Keycode.Del => KeyCodes.KEY_CE,
            Keycode.Escape => KeyCodes.KEY_CLR,
            Keycode.F1 => KeyCodes.KEY_A,
            Keycode.F2 => KeyCodes.KEY_B,
            Keycode.F3 => KeyCodes.KEY_C,
            Keycode.F4 => KeyCodes.KEY_D,
            Keycode.F5 => KeyCodes.KEY_E,
            _ => null
        };

        if (tiKey == null)
        {
            return base.OnKeyDown(keyCode, e);
        }

        App.ViewModel.PressKey(tiKey.Value);
        return true;
    }
}
