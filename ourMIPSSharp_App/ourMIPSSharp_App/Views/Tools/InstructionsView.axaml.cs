using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ourMIPSSharp_App.Views;

public partial class InstructionsView : UserControl {
    public InstructionsView() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (sender is DataGrid grid && e.AddedItems.Count > 0)
            grid.SelectedItems.Clear();
    }
}