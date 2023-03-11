#region

using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaEdit.Rendering;

#endregion

namespace ourMIPS_App.Views.Editor;

/// <summary>
/// Based on <a href="https://github.com/AvaloniaUI/AvaloniaEdit/blob/1937a74920098d1bac0cf45110bc6e0ab2998375/src/AvaloniaEdit/Rendering/CurrentLineHighlightRenderer.cs">this</a>.
/// </summary>
public class EditorDebugCurrentLineHighlighter : AvaloniaObject, IBackgroundRenderer {
    #region Fields

    private TextView _textView;

    public static readonly Color DefaultBackground = Colors.Transparent;
    public static readonly Color DefaultBorder = Colors.Beige;

    #endregion

    #region Properties

    public static readonly AttachedProperty<int> LineProperty =
        AvaloniaProperty.RegisterAttached<EditorDebugCurrentLineHighlighter, EditorDebugCurrentLineHighlighter, int>(
            "Line", 0, false, BindingMode.OneWay);

    public int Line {
        get => GetLine(this);
        set => SetLine(this, value);
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public IBrush BackgroundBrush { get; set; }

    public IPen BorderPen { get; set; }

    #endregion

    public EditorDebugCurrentLineHighlighter() {
        BorderPen = new ImmutablePen(new ImmutableSolidColorBrush(DefaultBorder), 1);
        BackgroundBrush = new ImmutableSolidColorBrush(DefaultBackground);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        base.OnPropertyChanged(change);
        _textView.InvalidateLayer(Layer);
    }

    public void Initialize(TextView textView) {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
        _textView.BackgroundRenderers.Add(this);
    }

    public void Draw(TextView textView, DrawingContext drawingContext) {
        var builder = new BackgroundGeometryBuilder();

        var visualLine = _textView.GetVisualLine(Line);
        if (visualLine == null) return;

        var linePosY = visualLine.VisualTop - _textView.ScrollOffset.Y;

        builder.AddRectangle(textView, new Rect(0, linePosY, textView.Bounds.Width, visualLine.Height));

        var geometry = builder.CreateGeometry();
        if (geometry != null) {
            drawingContext.DrawGeometry(BackgroundBrush, BorderPen, geometry);
        }
    }

    /// <summary>
    /// Accessor for Attached property <see cref="LineProperty"/>.
    /// </summary>
    public static void SetLine(AvaloniaObject element, int value) {
        element.SetValue(LineProperty, value);
    }

    /// <summary>
    /// Accessor for Attached property <see cref="LineProperty"/>.
    /// </summary>
    public static int GetLine(AvaloniaObject element) {
        return element.GetValue(LineProperty);
    }
}