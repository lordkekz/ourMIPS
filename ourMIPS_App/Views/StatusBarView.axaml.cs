#region

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

namespace ourMIPS_App.Views;

public partial class StatusBarView : UserControl {
    public StatusBarView() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}