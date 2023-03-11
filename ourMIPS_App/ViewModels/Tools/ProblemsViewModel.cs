#region

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Dock.Model.ReactiveUI.Controls;
using DynamicData.Binding;
using lib_ourMIPSSharp.Errors;
using ReactiveUI;

#endregion

namespace ourMIPS_App.ViewModels.Tools;

public class ProblemsViewModel : Tool {
    private readonly ObservableAsPropertyHelper<ObservableCollection<ProblemEntry>?> _entries;
    public ObservableCollection<ProblemEntry>? Entries => _entries.Value;
    public MainViewModel Main { get; }

    public ProblemsViewModel(MainViewModel main) {
        Main = main;
        Id = Title = "Problems";
        CanClose = CanFloat = CanPin = false;

        var entriesObservable = main.WhenAnyValue(m => m.CurrentFile.ProblemList);
        entriesObservable.ToProperty(this, x => x.Entries, out _entries);

        var anyProblemListChangedObservable = entriesObservable
            .Select(e => e.ObserveCollectionChanges().Select(_ => Unit.Default))
            .Merge();
        
        entriesObservable.Select(_ => Unit.Default).Merge(anyProblemListChangedObservable)
            .Subscribe(_ => { Title = $"Problems in current file ({Entries.Count})"; });
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