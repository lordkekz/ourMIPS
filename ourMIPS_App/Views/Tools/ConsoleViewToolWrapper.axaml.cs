#region

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ourMIPS_App.ViewModels.Tools;

#endregion

namespace ourMIPS_App.Views.Tools; 

public partial class ConsoleViewToolWrapper : UserControl {
    
    public ConsoleViewModelToolWrapper? ViewModel => DataContext as ConsoleViewModelToolWrapper;
    
    public ConsoleViewToolWrapper() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}