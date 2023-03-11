#region

using System;
using System.Linq;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

#endregion

namespace ourMIPS_App.Views.Editor; 

/// <summary>
/// Underlines Errors and Warnings in a DocumentView.
/// </summary>
public class ErrorHighlightingTransformer : DocumentColorizingTransformer {
    public DocumentView View { get; }

    public ErrorHighlightingTransformer(DocumentView dv) {
        View = dv;
    }

    protected override void ColorizeLine(DocumentLine line) {
        if (View.ViewModel.IsDebugging) return;

        var errors = View.ViewModel.ProblemList.Select(pe => (pe,
            // Start offset
            new TextLocation(pe.Error.Line, pe.Error.Column),
            // Effective end offset
            CurrentContext.Document.GetLocation(Math.Min(
                // Maximum allowed offset
                CurrentContext.Document.TextLength,
                // End offset according to Error
                CurrentContext.Document.GetOffset(pe.Error.Line, pe.Error.Column) + pe.Error.Length
            ))));

        var errorsAtLine = errors.Where(x => x.Item2.Line <= line.LineNumber && x.Item3.Line >= line.LineNumber);

        foreach (var (pe, a, b) in errorsAtLine) {
            ChangeLinePart(
                Math.Max(line.Offset, CurrentContext.Document.GetOffset(a)),
                Math.Min(line.EndOffset, CurrentContext.Document.GetOffset(b)),
                visualLine => {
                    var decoration = new TextDecoration {
                        Location = TextDecorationLocation.Underline,
                        Stroke = pe.IsWarning ? Brushes.Orange : Brushes.Red,
                        StrokeLineCap = PenLineCap.Round,
                        StrokeOffset = .05,
                        StrokeOffsetUnit = TextDecorationUnit.FontRenderingEmSize,
                        StrokeThickness = .1,
                        StrokeThicknessUnit = TextDecorationUnit.FontRenderingEmSize
                    };

                    TextDecorationCollection textDecorations;
                    if (visualLine.TextRunProperties.TextDecorations != null)
                        textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations)
                            { decoration };
                    else
                        textDecorations = new TextDecorationCollection { decoration };

                    visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                }
            );
        }
    }
}