using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using TI5X.Avalonia.ViewModels;

namespace TI5X.Avalonia.Controls;

public enum KeyStyle
{
    Standard, // Brown
    Number,   // White
    Yellow,   // Gold/Yellow
    TopRow    // Brown (A-E)
}

public partial class KeyButton : UserControl
{
    public static readonly StyledProperty<string> KeyTextProperty =
        AvaloniaProperty.Register<KeyButton, string>(nameof(KeyText), string.Empty);

    public static readonly StyledProperty<string> AltTextProperty =
        AvaloniaProperty.Register<KeyButton, string>(nameof(AltText), string.Empty);

    public static readonly StyledProperty<int> KeyCodeProperty =
        AvaloniaProperty.Register<KeyButton, int>(nameof(KeyCode), 0);

    public static readonly StyledProperty<KeyStyle> StyleTypeProperty =
        AvaloniaProperty.Register<KeyButton, KeyStyle>(nameof(StyleType), KeyStyle.Standard);

    public static readonly StyledProperty<IBrush> ButtonBackgroundProperty =
        AvaloniaProperty.Register<KeyButton, IBrush>(nameof(ButtonBackground), new SolidColorBrush(Color.FromRgb(48, 38, 34)));

    public static readonly StyledProperty<IBrush> ButtonForegroundProperty =
        AvaloniaProperty.Register<KeyButton, IBrush>(nameof(ButtonForeground), Brushes.White);

    public static readonly StyledProperty<IBrush> ButtonBorderBrushProperty =
        AvaloniaProperty.Register<KeyButton, IBrush>(nameof(ButtonBorderBrush), new SolidColorBrush(Color.FromRgb(70, 58, 52)));

    public string KeyText
    {
        get => GetValue(KeyTextProperty);
        set => SetValue(KeyTextProperty, value);
    }

    public string AltText
    {
        get => GetValue(AltTextProperty);
        set => SetValue(AltTextProperty, value);
    }

    public int KeyCode
    {
        get => GetValue(KeyCodeProperty);
        set => SetValue(KeyCodeProperty, value);
    }

    public KeyStyle StyleType
    {
        get => GetValue(StyleTypeProperty);
        set
        {
            SetValue(StyleTypeProperty, value);
            ApplyStyle(value);
        }
    }

    public IBrush ButtonBackground
    {
        get => GetValue(ButtonBackgroundProperty);
        set => SetValue(ButtonBackgroundProperty, value);
    }

    public IBrush ButtonForeground
    {
        get => GetValue(ButtonForegroundProperty);
        set => SetValue(ButtonForegroundProperty, value);
    }

    public IBrush ButtonBorderBrush
    {
        get => GetValue(ButtonBorderBrushProperty);
        set => SetValue(ButtonBorderBrushProperty, value);
    }

    public KeyButton()
    {
        InitializeComponent();
        ApplyStyle(StyleType);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StyleTypeProperty)
        {
            ApplyStyle((KeyStyle)(change.NewValue ?? KeyStyle.Standard));
        }
    }

    private void ApplyStyle(KeyStyle style)
    {
        switch (style)
        {
            case KeyStyle.Number:
                ButtonBackground = new SolidColorBrush(Color.FromRgb(240, 238, 230)); // Off-white
                ButtonForeground = new SolidColorBrush(Color.FromRgb(20, 20, 20));    // Black
                ButtonBorderBrush = new SolidColorBrush(Color.FromRgb(160, 155, 145));
                break;
            case KeyStyle.Yellow:
                ButtonBackground = new SolidColorBrush(Color.FromRgb(230, 180, 50));  // Gold/Yellow
                ButtonForeground = new SolidColorBrush(Color.FromRgb(20, 20, 20));    // Black
                ButtonBorderBrush = new SolidColorBrush(Color.FromRgb(180, 140, 30));
                break;
            case KeyStyle.Standard:
            case KeyStyle.TopRow:
            default:
                ButtonBackground = new SolidColorBrush(Color.FromRgb(55, 45, 40));    // Classic dark brown
                ButtonForeground = Brushes.White;                                     // White
                ButtonBorderBrush = new SolidColorBrush(Color.FromRgb(85, 70, 62));
                break;
        }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PressKey(KeyCode);
        }
    }
}
