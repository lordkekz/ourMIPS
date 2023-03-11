#region

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

namespace ourMIPS_App.Views; 

public partial class SettingsView : UserControl {
    public SettingsView() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}