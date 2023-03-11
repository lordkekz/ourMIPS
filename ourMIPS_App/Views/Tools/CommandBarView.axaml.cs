#region

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

namespace ourMIPS_App.Views.Tools; 

public partial class CommandBarView : UserControl {
    public CommandBarView() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}