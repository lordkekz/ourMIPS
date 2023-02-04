using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ourMIPSSharp_App.Views; 

public partial class CommandBarView : UserControl {
    public CommandBarView() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}