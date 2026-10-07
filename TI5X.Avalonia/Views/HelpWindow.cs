using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TI5X.Avalonia.Views;

public class HelpWindow : Window
{
    public HelpWindow()
    {
        Title = "TI5X Guide";
        Width = 600;
        Height = 450;
        Content = new ScrollViewer
        {
            Content = new TextBlock
            {
                Margin = new Thickness(24),
                TextWrapping = TextWrapping.Wrap,
                Text = "TI-59 / TI5X\n\n" +
                       "Use the keypad or your keyboard for numbers and arithmetic. Enter evaluates, Escape clears, Backspace clears the entry. F1–F5 select A–E.\n\n" +
                       "2nd selects the yellow secondary function; INV selects the inverse function. DEG, RAD and GRAD choose the angle unit.\n\n" +
                       "LRN enables program entry. SST steps through a program, RST resets the program counter, R/S starts or stops execution. STO and RCL take a two-digit register number.\n\n" +
                       "File opens and saves .ti5p programs and loads .ti5l library modules. View opens the virtual PC-100A printer and program debugger.\n\n" +
                       "The bundled library contains the available program bank; it is not the complete original Master Library."
            }
        };
    }
}