using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Views;

public partial class MainView : UserControl {
    public MainViewModel? ViewModel => DataContext as MainViewModel;
    public MainView()
    {
        InitializeComponent();
        
        Editor.Options.ShowBoxForControlCharacters = true;
        Editor.Options.ShowSpaces = false;
        Editor.Options.ShowTabs = false;
        Editor.Options.ShowEndOfLine = false;
        Editor.Options.HighlightCurrentLine = true;

        using (var reader = new XmlTextReader("../../../../ourMIPSSharp_App/Assets/ourMIPS.xshd")) {
            Editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        
        Editor.
        
        Editor.AddHandler(PointerWheelChangedEvent, (o, i) => {
            if (i.KeyModifiers != KeyModifiers.Control) return;
            if (i.Delta.Y > 0) Editor.FontSize++;
            else Editor.FontSize = Editor.FontSize > 1 ? Editor.FontSize - 1 : 1;
        }, RoutingStrategies.Bubble, true);
    }

    protected override void OnLoaded() {
        base.OnLoaded();
        
        var infoColor = this.TryFindResource("SystemBaseMediumColor", out var infoX) ? (Color) infoX : default;
        var normalColor = this.TryFindResource("SystemBaseHighColor", out var normalX) ? (Color) normalX : default;
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