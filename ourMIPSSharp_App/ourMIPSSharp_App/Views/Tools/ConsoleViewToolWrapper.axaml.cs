using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ourMIPSSharp_App.Views.Tools; 

public partial class ConsoleViewToolWrapper : UserControl {
    public ConsoleViewToolWrapper() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}