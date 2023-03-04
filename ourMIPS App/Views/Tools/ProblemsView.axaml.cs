using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ourMIPSSharp_App.ViewModels.Tools;
using ourMIPSSharp_App.Views.Editor;

namespace ourMIPSSharp_App.Views.Tools; 

public partial class ProblemsView : UserControl {
    public ProblemsViewModel? ViewModel => DataContext as ProblemsViewModel;
    
    public ProblemsView() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (sender is DataGrid grid && e.AddedItems.Count > 0) {
            grid.SelectedItems.Clear();

            if (e.AddedItems[0] is not ProblemEntry pe) return;
            var documentView = this.FindLogicalAncestorOfType<MainView>()!.FindDescendantOfType<DocumentView>()!;
            documentView.Editor.CaretOffset = documentView.Editor.Document.GetOffset(pe.Error.Line, pe.Error.Column);
            documentView.Editor.TextArea.Caret.BringCaretToView();
        }
    }
}