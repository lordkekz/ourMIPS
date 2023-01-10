using Avalonia.Controls;

namespace ourMIPSSharp_App.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        
        Editor.Options.ShowBoxForControlCharacters = true;
        Editor.Options.ShowSpaces = false;
        Editor.Options.ShowTabs = false;
        Editor.Options.ShowEndOfLine = false;
        Editor.Options.HighlightCurrentLine = true;
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (sender is DataGrid grid && e.AddedItems.Count > 0)
            grid.SelectedItems.Clear();
    }
}