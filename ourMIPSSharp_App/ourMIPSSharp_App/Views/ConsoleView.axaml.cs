using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.TextMate.Grammars;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Views;

using Pair = KeyValuePair<int, Control>;

public partial class ConsoleView : UserControl {
    public ConsoleViewModel? ViewModel => DataContext as ConsoleViewModel;

    public static readonly AttachedProperty<IList<Brush>> LineBrushesProperty =
        AvaloniaProperty.RegisterAttached<ConsoleView, ConsoleView, IList<Brush>>(
            "LineBrushes", default(IList<Brush>), false, BindingMode.OneTime);

    private TextEditor _editor;

    public IList<IBrush> LineBrushes { get; set; }

    private bool _autoScrollActive;

    public ConsoleView() {
        InitializeComponent();

        _editor = this.FindControl<TextEditor>("Editor")!;
        _editor.Options.ShowBoxForControlCharacters = true;
        _editor.Options.ShowSpaces = false;
        _editor.Options.ShowTabs = false;
        _editor.Options.ShowEndOfLine = false;
        // _editor.Options.EndOfLineCRLFGlyph =
        //     _editor.Options.EndOfLineCRGlyph =
        //         _editor.Options.EndOfLineLFGlyph = "*";
        _editor.Options.HighlightCurrentLine = false;
        _editor.Options.AllowScrollBelowDocument = false;

        _editor.Background = Brushes.Transparent;
        _editor.ContextMenu = new ContextMenu {
            Items = new List<MenuItem> {
                new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
                new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) }
            }
        };
        _editor.TextArea.Background = this.Background;
        _editor.Options.ShowBoxForControlCharacters = true;
        _editor.TextArea.RightClickMovesCaret = true;

        _editor.TextArea.TextView.LineTransformers.Add(new ConsoleColorTransformer() { MyConsoleView = this });

        this.AddHandler(PointerWheelChangedEvent, (o, i) => {
            if (i.KeyModifiers != KeyModifiers.Control) return;
            if (i.Delta.Y > 0) _editor.FontSize++;
            else _editor.FontSize = _editor.FontSize > 1 ? _editor.FontSize - 1 : 1;
        }, RoutingStrategies.Bubble, true);


        // Autoscroll behavior
        _editor.TextArea.Caret.PositionChanged += (sender, args) => {
            _autoScrollActive = _editor.CaretOffset >= _editor.Document.TextLength;
            Debug.WriteLine(_autoScrollActive);
        };
        _editor.TextChanged += (sender, args) => {
            if (!_autoScrollActive) return;
            _editor.CaretOffset = _editor.Document.TextLength;
        };
    }

    protected override void OnDataContextChanged(EventArgs e) {
        base.OnDataContextChanged(e);
        if (ViewModel is null) return;
        ViewModel!.LinesFlushed += (sender, args) => {
            Debug.WriteLine(_editor.TextArea.Caret.Line);
            _editor.TextArea.Caret.BringCaretToView();
        };
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private class ConsoleColorTransformer : DocumentColorizingTransformer {
        public ConsoleView? MyConsoleView { get; init; }

        protected override void ColorizeLine(DocumentLine line) {
            if (MyConsoleView.ViewModel.Document.TextLength == 0 ||
                MyConsoleView.ViewModel.Document.LineCount <= line.LineNumber ||
                MyConsoleView.LineBrushes is null)
                return;

            // try {
            var colorHint = MyConsoleView.ViewModel.GetColorHint(line.LineNumber);
            var brush = MyConsoleView.LineBrushes[colorHint];
            // Debug.WriteLine($"{line.LineNumber} : {colorHint} => {brush}");

            ChangeLinePart(
                line.Offset,
                line.EndOffset,
                visualLine => visualLine.TextRunProperties.SetForegroundBrush(brush)
            );
            // }
            // catch (NullReferenceException) { }
            // catch (ArgumentOutOfRangeException) { }
            // catch (IndexOutOfRangeException) { }
        }
    }

    private void InputBox_OnKeyDown(object? sender, KeyEventArgs e) {
        if (e is { KeyModifiers: KeyModifiers.None, Key: Key.Enter or Key.Return }) {
            ViewModel.SubmitInput();
            // InputBox.Clear();
        }
    }
}