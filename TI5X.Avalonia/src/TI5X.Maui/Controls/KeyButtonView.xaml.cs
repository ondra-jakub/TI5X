namespace TI5X.Maui.Controls;

public enum KeyStyle
{
    Standard,
    Number,
    Yellow,
    TopRow
}

public partial class KeyButtonView : ContentView
{
    public static readonly BindableProperty KeyTextProperty =
        BindableProperty.Create(nameof(KeyText), typeof(string), typeof(KeyButtonView), string.Empty);

    public static readonly BindableProperty AltTextProperty =
        BindableProperty.Create(nameof(AltText), typeof(string), typeof(KeyButtonView), string.Empty);

    public static readonly BindableProperty KeyCodeProperty =
        BindableProperty.Create(nameof(KeyCode), typeof(int), typeof(KeyButtonView), 0);

    public static readonly BindableProperty StyleTypeProperty =
        BindableProperty.Create(nameof(StyleType), typeof(KeyStyle), typeof(KeyButtonView), KeyStyle.Standard,
            propertyChanged: (bindable, _, _) => ((KeyButtonView)bindable).ApplyStyle());

    public string KeyText
    {
        get => (string)GetValue(KeyTextProperty);
        set => SetValue(KeyTextProperty, value);
    }

    public string AltText
    {
        get => (string)GetValue(AltTextProperty);
        set => SetValue(AltTextProperty, value);
    }

    public int KeyCode
    {
        get => (int)GetValue(KeyCodeProperty);
        set => SetValue(KeyCodeProperty, value);
    }

    public KeyStyle StyleType
    {
        get => (KeyStyle)GetValue(StyleTypeProperty);
        set => SetValue(StyleTypeProperty, value);
    }

    public event EventHandler? Clicked;

    public KeyButtonView()
    {
        InitializeComponent();
        BindingContext = this;
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        switch (StyleType)
        {
            case KeyStyle.Number:
                KeyButton.Background = new SolidColorBrush(Color.FromRgb(240, 238, 230));
                KeyButton.BorderColor = Color.FromRgb(160, 155, 145);
                KeyButton.TextColor = Color.FromRgb(20, 20, 20);
                break;
            case KeyStyle.Yellow:
                KeyButton.Background = new SolidColorBrush(Color.FromRgb(230, 180, 50));
                KeyButton.BorderColor = Color.FromRgb(180, 140, 30);
                KeyButton.TextColor = Color.FromRgb(20, 20, 20);
                break;
            case KeyStyle.Standard:
            case KeyStyle.TopRow:
            default:
                KeyButton.Background = new SolidColorBrush(Color.FromRgb(55, 45, 40));
                KeyButton.BorderColor = Color.FromRgb(85, 70, 62);
                KeyButton.TextColor = Colors.White;
                break;
        }
    }

    private void OnButtonClicked(object? sender, EventArgs e) => Clicked?.Invoke(this, EventArgs.Empty);
}
