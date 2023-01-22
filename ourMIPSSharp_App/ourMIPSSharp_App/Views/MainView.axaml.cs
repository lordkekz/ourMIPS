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
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Rendering;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Views;

using Pair = KeyValuePair<int, Control>;

public partial class MainView : UserControl {
    public MainViewModel? ViewModel => DataContext as MainViewModel;

    private ElementGenerator _generator;
    private readonly BreakPointMargin _breakPointMargin;

    public MainView() {
        InitializeComponent();

        Editor.Options.ShowBoxForControlCharacters = true;
        Editor.Options.ShowSpaces = false;
        Editor.Options.ShowTabs = false;
        Editor.Options.ShowEndOfLine = false;
        Editor.Options.HighlightCurrentLine = true;

        // Load syntax highlighting definition as resource so it's always available
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>()!;
        using (var bitmap = assets.Open(new Uri("avares://ourMIPSSHarp_App/Assets/ourMIPS.xshd"))) {
            using (var reader = new XmlTextReader(bitmap)) {
                Editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
        
        Editor.TextArea.Caret.PositionChanged += (sender, args) => {
            ViewModel?.UpdateCaretInfo(
                Editor.TextArea.Caret.Position.Line,
                Editor.TextArea.Caret.Position.Column);
        };

        // _generator = new ElementGenerator() { Editor = Editor };
        // Editor.TextArea.TextView.ElementGenerators.Add(_generator);

        Editor.AddHandler(PointerWheelChangedEvent, (o, i) => {
            if (i.KeyModifiers != KeyModifiers.Control) return;
            i.Handled = true;
            if (i.Delta.Y > 0) Editor.FontSize++;
            else Editor.FontSize = Editor.FontSize > 1 ? Editor.FontSize - 1 : 1;

            _breakPointMargin?.InvalidateMeasure();
        }, RoutingStrategies.Tunnel, true);

        _breakPointMargin = new BreakPointMargin(Editor, this);
        Editor.TextArea.LeftMargins.Insert(0, _breakPointMargin);
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

    // TODO eventually repurpose to show inline code intelligence
    class ElementGenerator : VisualLineElementGenerator {
        public List<Control> controls = new();
        public TextEditor Editor { get; init; }

        /// <summary>
        /// Gets the first interested offset using binary search
        /// </summary>
        /// <returns>The first interested offset.</returns>
        /// <param name="startOffset">Start offset.</param>
        public override int GetFirstInterestedOffset(int startOffset) {
            // Find next newline
            if (startOffset == Editor.Document.GetLineByOffset(startOffset).Offset)
                return startOffset;
            return -1;
        }

        public override VisualLineElement ConstructElement(int offset) {
            var line = Editor.Document.GetLineByOffset(offset).LineNumber - 1;
            var c = line < controls.Count
                ? controls[line]
                : new Ellipse() {
                    Height = 12, Width = 12, VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center, Fill = Brushes.Red
                };
            return new InlineObjectElement(0, c);
        }
    }
}