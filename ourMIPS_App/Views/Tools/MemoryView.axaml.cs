#region

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

namespace ourMIPS_App.Views.Tools; 

public partial class MemoryView : UserControl {
    public MemoryView() {
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