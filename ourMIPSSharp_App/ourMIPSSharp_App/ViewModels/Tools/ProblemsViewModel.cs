using System.Collections;
using System.Collections.ObjectModel;
using Dock.Model.ReactiveUI.Controls;
using lib_ourMIPSSharp.Errors;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels.Tools;

public class ProblemsViewModel : Tool {
    private readonly ObservableAsPropertyHelper<ObservableCollection<ProblemEntry>?> _entries;
    public ObservableCollection<ProblemEntry>? Entries => _entries.Value;

    public ProblemsViewModel(MainViewModel main) {
        Id = Title = "Problems";
        CanClose = CanFloat = CanPin = false;

        main.WhenAnyValue(m => m.DebugSession!.Editor.ProblemList)
            .ToProperty(this, x => x.Entries, out _entries);
    }
}

public record ProblemEntry(CompilerError Error) {
    public bool IsError => Error.Severity == CompilerSeverity.Error;
    public bool IsWarning => Error.Severity == CompilerSeverity.Warning;
    public string Pos => $"{Error.Line}:{Error.Column}";
    public string? Message => Error.Message;
}

public sealed class ProblemEntryPositionComparer : IComparer {
    public int Compare(object? a, object? b) {
        if (ReferenceEquals(a, b)) return 0;
        if (ReferenceEquals(null, a)) return 1;
        if (ReferenceEquals(null, b)) return -1;
        if (a is not ProblemEntry x || b is not ProblemEntry y) return 0;
        var lineComparison = x.Error.Line.CompareTo(y.Error.Line);
        if (lineComparison != 0) return lineComparison;
        var columnComparison = x.Error.Column.CompareTo(y.Error.Column);
        return columnComparison;
    }
}