using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Views;

/// <summary>
/// Represents the left editor Margin that contains breakpoints.
/// See <a href="https://github.com/AvaloniaUI/AvaloniaEdit/issues/76">this</a> issue on AvaloniaEdit.
/// This class is based on <a href="https://github.com/VitalElement/AvalonStudio/blob/master/AvalonStudio/AvalonStudio.Controls.Editor/BreakPointMargin.cs">BreakPointMargin</a> from AvalonStudio.
/// </summary>
public class BreakPointMargin : AbstractMargin {
    public MainViewModel ViewModel => _mv.ViewModel;
    private int previewLine;
    private bool previewPointVisible;
    private readonly TextEditor _editor;
    private readonly MainView _mv;

    static BreakPointMargin() {
        FocusableProperty.OverrideDefaultValue(typeof(BreakPointMargin), true);
    }

    public BreakPointMargin(TextEditor editor, MainView mv) {
        _editor = editor;
        _mv = mv;
    }

    public override void Render(DrawingContext context) {
        if (!TextView.VisualLinesValid) return;
        context.FillRectangle(Brushes.Black, Bounds);
        context.DrawLine(new Pen(Brushes.White, 0.5), Bounds.TopRight, Bounds.BottomRight);

        if (TextView.VisualLines.Count <= 0) return;

        var firstLine = TextView.VisualLines.FirstOrDefault();
        var height = firstLine.Height;

        foreach (var breakPoint in ViewModel.UIBreakpoints) {
            var visualLine =
                TextView.VisualLines.FirstOrDefault(vl => vl.FirstDocumentLine.LineNumber == breakPoint.Line);

            if (visualLine != null) {
                context.FillRectangle(Brush.Parse("#FF3737"),
                    new Rect((Bounds.Size.Width / 4) - 1,
                        visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0],
                            AvaloniaEdit.Rendering.VisualYPosition.LineTop) + (Bounds.Size.Width / 4) -
                        TextView.VerticalOffset,
                        Bounds.Size.Width / 1.5, height / 1.5), (float)height);
            }
        }

        if (previewPointVisible) {
            var visualLine =
                TextView.VisualLines.FirstOrDefault(vl => vl.FirstDocumentLine.LineNumber == previewLine);

            if (visualLine != null) {
                context.FillRectangle(Brush.Parse("#E67466"),
                    new Rect((Bounds.Size.Width / 4) - 1,
                        visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0],
                            AvaloniaEdit.Rendering.VisualYPosition.LineTop) + (Bounds.Size.Width / 4) -
                        TextView.VerticalOffset,
                        Bounds.Size.Width / 1.5, height / 1.5), (float)height);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e) {
        previewPointVisible = true;

        var textView = TextView;

        var textViewPosition = _editor.GetPositionFromPoint(e.GetPosition(this));
        if (!textViewPosition.HasValue) return;

        var offset = _editor.Document.GetOffset(textViewPosition.Value.Location);

        if (offset != -1) {
            previewLine =
                textView.Document.GetLineByOffset(offset).LineNumber; // convert from text line to visual line.
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e) {
        previewPointVisible = true;

        var textView = TextView;

        var textViewPosition = _editor.GetPositionFromPoint(e.GetPosition(this));
        if (!textViewPosition.HasValue) return;

        var offset = _editor.Document.GetOffset(textViewPosition.Value.Location);

        if (offset != -1) {
            var lineClicked = -1;
            lineClicked =
                textView.Document.GetLineByOffset(offset).LineNumber; // convert from text line to visual line.

            var currentBreakPoint =
                ViewModel.UIBreakpoints.FirstOrDefault(bp => bp.Line == lineClicked);

            if (currentBreakPoint != null) {
                ViewModel.UIBreakpoints.Remove(currentBreakPoint);
                currentBreakPoint.IsDeleted = true;
            }
            else {
                if (!string.IsNullOrEmpty(_editor.Text)) {
                    var a = Document.CreateAnchor(offset);
                    var bp = new Breakpoint(x => {
                        x.Line = a.Line;
                        x.IsDeleted |= a.IsDeleted;
                    });
                    ViewModel.UIBreakpoints.Add(bp);
                }
            }
        }

        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) {
        return TextView != null ? new Size(TextView.DefaultLineHeight, 0) : new Size(0, 0);
    }

    protected override void OnPointerExited(PointerEventArgs e) {
        previewPointVisible = false;

        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e) {
        InvalidateVisual();
        e.Handled = true;
    }
}