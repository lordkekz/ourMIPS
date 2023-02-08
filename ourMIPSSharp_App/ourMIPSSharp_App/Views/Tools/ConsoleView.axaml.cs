using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using ourMIPSSharp_App.ViewModels.Tools;

namespace ourMIPSSharp_App.Views.Tools;

public partial class ConsoleView : UserControl {
    public ConsoleViewModel? ViewModel => DataContext as ConsoleViewModel;

    public static readonly AttachedProperty<IList<IBrush>?> LineBrushesProperty =
        AvaloniaProperty.RegisterAttached<ConsoleView, ConsoleView, IList<IBrush>?>(
            "LineBrushes", default, false, BindingMode.OneTime);

    public IList<IBrush>? LineBrushes {
        get => GetLineBrushes(this);
        set => SetLineBrushes(this, value);
    }

    public static readonly AttachedProperty<bool> IsAutoScrollEnabledProperty =
        AvaloniaProperty.RegisterAttached<ConsoleView, ConsoleView, bool>(
            "IsAutoScrollEnabled", true, false, BindingMode.TwoWay);

    public bool IsAutoScrollEnabled {
        get => GetIsAutoScrollEnabled(this);
        set => SetIsAutoScrollEnabled(this, value);
    }

    private TextEditor _editor;

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

        AddHandler(PointerWheelChangedEvent, (o, i) => {
            if (i.KeyModifiers != KeyModifiers.Control) {
                IsAutoScrollEnabled = false;
                return;
            }

            i.Handled = true;
            if (i.Delta.Y > 0) _editor.FontSize++;
            else _editor.FontSize = _editor.FontSize > 1 ? _editor.FontSize - 1 : 1;
        }, RoutingStrategies.Tunnel, true);


        var inputBox = this.FindControl<TextBox>("InputBox")!;
        inputBox.PropertyChanged += (sender, args) => {
            if (args.Property != IsEnabledProperty) return;
            if (inputBox.IsEnabled)
                inputBox.Focus();
            else
                _editor.Focus();
        };
        
        this.TryFindColor("SystemBaseMediumColor", out var infoColor);
        this.TryFindColor("SystemBaseHighColor", out var normalColor);
        LineBrushes = new List<IBrush>() {
            new SolidColorBrush(infoColor),
            new SolidColorBrush(normalColor),
            Brushes.OrangeRed,
            Brushes.LimeGreen
        };
    }

    protected override void OnDataContextChanged(EventArgs e) {
        base.OnDataContextChanged(e);
        if (ViewModel is null) return;
        ViewModel!.LinesFlushed += async (sender, args) => {
            if (!IsAutoScrollEnabled)
                return;
            _editor.CaretOffset = _editor.Document.TextLength;

            // Release UI thread to allow Editor to calculate new line heights
            await Task.Delay(10);
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
        }
    }

    /// <summary>
    /// Accessor for Attached property <see cref="LineBrushesProperty"/>.
    /// </summary>
    public static void SetLineBrushes(AvaloniaObject element, IList<IBrush> value) {
        element.SetValue(LineBrushesProperty, value);
    }

    /// <summary>
    /// Accessor for Attached property <see cref="LineBrushesProperty"/>.
    /// </summary>
    public static IList<IBrush>? GetLineBrushes(AvaloniaObject element) {
        return element.GetValue(LineBrushesProperty);
    }

    /// <summary>
    /// Accessor for Attached property <see cref="IsAutoScrollEnabledProperty"/>.
    /// </summary>
    public static void SetIsAutoScrollEnabled(AvaloniaObject element, bool value) {
        element.SetValue(IsAutoScrollEnabledProperty, value);
    }

    /// <summary>
    /// Accessor for Attached property <see cref="IsAutoScrollEnabledProperty"/>.
    /// </summary>
    public static bool GetIsAutoScrollEnabled(AvaloniaObject element) {
        return element.GetValue(IsAutoScrollEnabledProperty);
    }
}