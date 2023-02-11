using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using ourMIPSSharp_App.ViewModels.Tools;

namespace ourMIPSSharp_App.Views.Tools; 

public partial class ConsoleViewToolWrapper : UserControl {
    
    public ConsoleViewModelToolWrapper? ViewModel => DataContext as ConsoleViewModelToolWrapper;
    
    public ConsoleViewToolWrapper() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}