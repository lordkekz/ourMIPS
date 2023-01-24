using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaEdit.Rendering;

namespace ourMIPSSharp_App.Rendering;

/// <summary>
/// Based on <a href="https://github.com/AvaloniaUI/AvaloniaEdit/blob/1937a74920098d1bac0cf45110bc6e0ab2998375/src/AvaloniaEdit/Rendering/CurrentLineHighlightRenderer.cs">this</a>.
/// </summary>
public class EditorDebugCurrentLineHighlighter : IBackgroundRenderer {

    #region Fields

    private int _line;
    private readonly TextView _textView;

    public static readonly Color DefaultBackground = Colors.Transparent;
    public static readonly Color DefaultBorder = Colors.Beige;

    #endregion

    #region Properties

    public int Line {
        get { return _line; }
        set {
            if (_line != value) {
                _line = value;
                _textView.InvalidateLayer(Layer);
            }
        }
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public IBrush BackgroundBrush { get; set; }

    public IPen BorderPen { get; set; }

    #endregion

    public EditorDebugCurrentLineHighlighter(TextView textView) {
        BorderPen = new ImmutablePen(new ImmutableSolidColorBrush(DefaultBorder), 1);

        BackgroundBrush = new ImmutableSolidColorBrush(DefaultBackground);

        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
        _textView.BackgroundRenderers.Add(this);

        _line = 0;
    }

    public void Draw(TextView textView, DrawingContext drawingContext) {
        if (!_textView.Options.HighlightCurrentLine)
            return;

        var builder = new BackgroundGeometryBuilder();

        var visualLine = _textView.GetVisualLine(_line);
        if (visualLine == null) return;

        var linePosY = visualLine.VisualTop - _textView.ScrollOffset.Y;

        builder.AddRectangle(textView, new Rect(0, linePosY, textView.Bounds.Width, visualLine.Height));

        var geometry = builder.CreateGeometry();
        if (geometry != null) {
            drawingContext.DrawGeometry(BackgroundBrush, BorderPen, geometry);
        }
    }
}