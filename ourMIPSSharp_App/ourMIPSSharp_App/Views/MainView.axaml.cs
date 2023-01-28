using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Rendering;
using ourMIPSSharp_App.Rendering;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Views;

using Pair = KeyValuePair<int, Control>;

public partial class MainView : UserControl {
    public MainViewModel? ViewModel => DataContext as MainViewModel;
    
    private readonly BreakPointMargin _breakPointMargin;
    private readonly EditorDebugCurrentLineHighlighter _lineHightlighter;

    public MainView() {
        InitializeComponent();
    }

    protected override void OnLoaded() {
        base.OnLoaded();

        this.TryFindColor("SystemBaseMediumColor", out var infoColor);
        this.TryFindColor("SystemBaseHighColor", out var normalColor);
        ConView.LineBrushes = new List<IBrush>() {
            new SolidColorBrush(infoColor),
            new SolidColorBrush(normalColor),
            Brushes.OrangeRed,
            Brushes.LimeGreen
        };
        // Write ready text to console (immediately applies colors)
        ViewModel.Console.Clear();
        ViewModel.Backend.TextInfoWriter.WriteLine("Ready.");
        ViewModel.Console.FlushNewLines();
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (sender is DataGrid grid && e.AddedItems.Count > 0)
            grid.SelectedItems.Clear();
    }
}