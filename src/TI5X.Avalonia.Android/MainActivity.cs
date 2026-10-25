using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Avalonia.Android;
using TI5X.Avalonia.Core;
using TI5X.Avalonia.Services;

namespace TI5X.Avalonia.Android;

[Activity(
    Theme = "@style/MyTheme.NoActionBar",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
    // Must be registered before base.OnCreate: the Avalonia view and the shared
    // CalculatorPage (created via MainViewFactory) become interactive inside it.
    private AndroidFileService? _fileService;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        _fileService = new AndroidFileService(this);
        AppServices.FileService = _fileService;
        AppServices.Exit = FinishAffinity;
        AppServices.KeyPressFeedback = ProvideKeyFeedback;
        AppServices.DiagnosticLog = message => Log.Info("TI5X", message);
        ActivityResult = HandleActivityResult;

        // Phones stay portrait (the Activity attribute); large tablets may rotate so the
        // wide layout can show the printer beside the calculator (CalculatorShell).
        if (Resources?.Configuration?.SmallestScreenWidthDp is >= 600)
        {
            RequestedOrientation = ScreenOrientation.Unspecified;
        }

        base.OnCreate(savedInstanceState);
    }

    /// <summary>
    /// Short haptic tick for on-screen key presses (invoked from the shared
    /// KeyButton control via AppServices.KeyPressFeedback; null on desktop).
    /// </summary>
    private void ProvideKeyFeedback()
    {
        try
        {
            Vibrator? vibrator = OperatingSystem.IsAndroidVersionAtLeast(31)
                ? (GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator
                : GetVibratorLegacy();

            if (vibrator?.HasVibrator != true) return;

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                vibrator.Vibrate(VibrationEffect.CreateOneShot(25, VibrationEffect.DefaultAmplitude));
            }
            else
            {
                VibrateLegacy(vibrator);
            }
        }
        catch (Exception)
        {
            // Haptics are best-effort; a key press must never fail on devices
            // without a vibrator or with a broken vibrator service.
        }
    }

#pragma warning disable CA1422 // legacy vibrator APIs: used only below their deprecation level
    private Vibrator? GetVibratorLegacy()
        => (Vibrator?)GetSystemService(Context.VibratorService);

    private static void VibrateLegacy(Vibrator vibrator)
        => vibrator.Vibrate(25);
#pragma warning restore CA1422

    private void HandleActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        _fileService?.HandleResult(requestCode, resultCode, data);
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

        AppServices.RequireMain().PressKey(tiKey.Value);
        return true;
    }
}