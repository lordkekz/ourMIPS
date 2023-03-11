#region

using System;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using ourMIPS_App.Models;
using ourMIPS_App.ViewModels.Editor;

#endregion

namespace ourMIPS_App.Views.Editor;

/// <summary>
/// Represents the left editor Margin that contains breakpoints.
/// See <a href="https://github.com/AvaloniaUI/AvaloniaEdit/issues/76">this</a> issue on AvaloniaEdit.
/// This class is based on <a href="https://github.com/VitalElement/AvalonStudio/blob/master/AvalonStudio/AvalonStudio.Controls.Editor/BreakPointMargin.cs">BreakPointMargin</a> from AvalonStudio.
/// </summary>
public class BreakPointMargin : AbstractMargin {
    public DebugSessionViewModel? ViewModel => _mv.ViewModel?.DebugSession;
    private int _previewLine;
    private bool _previewPointVisible;
    private readonly TextEditor _editor;
    private readonly DocumentView _mv;

    static BreakPointMargin() {
        FocusableProperty.OverrideDefaultValue(typeof(BreakPointMargin), true);
    }

    public BreakPointMargin(TextEditor editor, DocumentView mv) {
        _editor = editor;
        _mv = mv;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public override void Render(DrawingContext context) {
        if (!TextView.VisualLinesValid) return;

        this.TryFindColor("SystemChromeBlackHighColor", out var bgColor);
        context.FillRectangle(new SolidColorBrush(bgColor), Bounds);
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
                            VisualYPosition.LineTop) + (Bounds.Size.Width / 4) -
                        TextView.VerticalOffset,
                        Bounds.Size.Width / 1.5, height / 1.5), (float)height);
            }
        }

        if (_previewPointVisible) {
            var visualLine =
                TextView.VisualLines.FirstOrDefault(vl => vl.FirstDocumentLine.LineNumber == _previewLine);

            if (visualLine != null) {
                context.FillRectangle(Brush.Parse("#E67466"),
                    new Rect((Bounds.Size.Width / 4) - 1,
                        visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0],
                            VisualYPosition.LineTop) + (Bounds.Size.Width / 4) -
                        TextView.VerticalOffset,
                        Bounds.Size.Width / 1.5, height / 1.5), (float)height);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e) {
        var textView = TextView;

        var textViewPosition = _editor.GetPositionFromPoint(e.GetPosition(this));
        if (!textViewPosition.HasValue) return;

        var offset = _editor.Document.GetOffset(textViewPosition.Value.Location);

        if (offset != -1) {
            _previewLine =
                textView.Document.GetLineByOffset(offset).LineNumber; // convert from text line to visual line.
            _previewPointVisible = LineHasInstruction(_previewLine);
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e) {
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
                _previewPointVisible = true;
            }
            else {
                if (LineHasInstruction(lineClicked)) {
                    var a = Document.CreateAnchor(offset);
                    var bp = new Breakpoint(x => {
                        // TODO this is broken. Breakpoint movement behavior is currently undefined when switching between DebugDocument and MainDocument.
                        try {
                            x.Line = a.Line;
                            x.IsDeleted |= a.IsDeleted;
                        }
                        catch (Exception) { }
                    });
                    ViewModel.UIBreakpoints.Add(bp);
                }
            }
        }

        InvalidateVisual();
    }

    private bool LineHasInstruction(int lineClicked) {
        var line = TextView.Document.GetLineByNumber(lineClicked);
        var lineStr = TextView.Document.GetText(line.Offset, line.EndOffset - line.Offset);
        var instructionWord = lineStr.Trim().Split(' ', 2)[0];
        if (string.IsNullOrWhiteSpace(instructionWord)) return false;
        var keyword = KeywordHelper.FromToken(new Token(DialectOptions.None)
            { Type = TokenType.Word, Content = instructionWord });
        return (uint)keyword > 4;
    }

    protected override Size MeasureOverride(Size availableSize) {
        if (TextView is null) return new Size(0, 0);

        var width = TextView.VisualLinesValid ? TextView.VisualLines.FirstOrDefault()?.Height : null;
        width ??= TextView.DefaultLineHeight;
        return new Size(width.Value, 0);
    }

    protected override void OnPointerExited(PointerEventArgs e) {
        _previewPointVisible = false;

        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e) {
        InvalidateVisual();
        e.Handled = true;
    }
}