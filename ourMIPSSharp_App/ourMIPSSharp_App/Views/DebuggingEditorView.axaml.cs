using System;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using ourMIPSSharp_App.DebugEditor;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Views;

public partial class DebuggingEditorView : UserControl {
    public MainViewModel? ViewModel => DataContext as MainViewModel;

    private readonly BreakPointMargin _breakPointMargin;
    private readonly EditorDebugCurrentLineHighlighter _lineHighlighter;

    public DebuggingEditorView() {
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
            ViewModel?.CurrentEditor?.UpdateCaretInfo(
                Editor.TextArea.Caret.Position.Line,
                Editor.TextArea.Caret.Position.Column);
        };

        _lineHighlighter = new EditorDebugCurrentLineHighlighter(Editor.TextArea.TextView);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlighter);

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

    protected override void OnDataContextChanged(EventArgs e) {
        base.OnDataContextChanged(e);
        if (ViewModel is null) return;
        var vm = ViewModel;
        vm.FileOpened += (s1, a1) => {
            if (a1 is null) return;
            var d = vm.CurrentDebugger!.DebuggerInstance;
            d.DebuggerBreaking += (s2, a2) => {
                if (vm.CurrentDebugger?.DebuggerInstance != d) return;
                _lineHighlighter.Line = a2.Line;
                Editor.CaretOffset = Editor.Document.Lines[a2.Line - 1].EndOffset;
                Editor.TextArea.Caret.BringCaretToView();
            };
            d.DebuggerBreakEnding += (sender, args) => {
                if (vm.CurrentDebugger?.DebuggerInstance != d) return;
                _lineHighlighter.Line = 0;
            };
        };
    }
}