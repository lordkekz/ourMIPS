#region

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using Dock.Model.ReactiveUI.Controls;
using ourMIPS_App.Models;
using ReactiveUI;

#endregion

namespace ourMIPS_App.ViewModels.Tools;

public class ConsoleViewModel : Tool {
    private string _inputString = "";

    public string InputString {
        get => _inputString;
        set => this.RaiseAndSetIfChanged(ref _inputString, value);
    }

    private TextDocument _document = new();

    public TextDocument Document {
        get => _document;
        set => this.RaiseAndSetIfChanged(ref _document, value);
    }

    private ConcurrentDictionary<int, int> ColorHints { get; } = new();

    public FileBackend Backend { get; }

    private readonly ConcurrentQueue<string> _newLines = new();

    public event EventHandler? LinesFlushed;

    private bool _isExpectingInput;
    private DateTime _lastFlush = DateTime.UnixEpoch;

    public bool IsExpectingInput {
        get => _isExpectingInput;
        set => this.RaiseAndSetIfChanged(ref _isExpectingInput, value);
    }

    protected virtual void OnLinesFlushed() {
        LinesFlushed?.Invoke(this, EventArgs.Empty);
    }

    public ConsoleViewModel(FileBackend backend) {
        Backend = backend;
        Backend.TextInfoWriter.LineWritten += TextInfoWriterOnLineWritten;
        Backend.TextOutWriter.LineWritten += TextOutWriterOnLineWritten;
        Backend.TextErrWriter.LineWritten += TextErrWriterOnLineWritten;
        Backend.TextInWriter.LineWritten += TextInWriterOnLineWritten;
    }

    private void TextInfoWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 0;
        _newLines.Enqueue(e.Content);
        if (ShouldAutoUpdateConsole())
            FlushNewLines().Wait();
    }

    private void TextOutWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 1;
        _newLines.Enqueue(e.Content);
        if (ShouldAutoUpdateConsole())
            FlushNewLines().Wait();
    }

    private void TextErrWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 2;
        _newLines.Enqueue(e.Content);
        if (ShouldAutoUpdateConsole())
            FlushNewLines().Wait();
    }

    private void TextInWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 3;
        _newLines.Enqueue("Input: " + e.Content);
        if (ShouldAutoUpdateConsole())
            FlushNewLines().Wait();
    }

    public bool HasNewLines => !_newLines.IsEmpty;

    /// <summary>
    /// Flushes new lines to UI. Automatically switches to UI thread.
    /// </summary>
    public async Task FlushNewLines() => await Observable.Start(DoFlushNewLines, RxApp.MainThreadScheduler);

    /// <summary>
    /// Flushes new lines to UI. Must be called from UI thread.
    /// </summary>
    public void DoFlushNewLines() {
        // There is no point in using DocumentTextWriter, since it also relies on Document.Insert
        // Only call Document.Insert once, since it seems to update ui every time
        var len = _newLines.Sum(s => s.Length);
        var str = new StringBuilder(len);
        foreach (var line in _newLines)
            str.Append(line);

        _newLines.Clear();
        Document.Insert(Document.TextLength, str.ToString());
        OnLinesFlushed();
        _lastFlush = DateTime.Now;
    }

    private bool ShouldAutoUpdateConsole()
        => HasNewLines && (DateTime.Now - _lastFlush).TotalMilliseconds > 100;

    public int GetColorHint(int lineNumber) => ColorHints[lineNumber - 1];

    /// <summary>
    /// Clears console. Must be called from UI thread.
    /// </summary>
    public void Clear() {
        // Make sure Writers are emptied
        Backend.TextInfoWriter.WriteLine();
        Backend.TextOutWriter.WriteLine();
        Backend.TextErrWriter.WriteLine();
        Backend.TextInWriter.WriteLine();

        // Clear Document and Hints
        Document.Text = "";
        _newLines.Clear();
        ColorHints.Clear();
    }

    /// <summary>
    /// Submits input. Must be called from UI thread.
    /// </summary>
    public void SubmitInput() {
        IsExpectingInput = false;
        Backend.TextInWriter.WriteLine(InputString);
        DoFlushNewLines();
        InputString = "";
    }

    /// <summary>
    /// Gets input. Must be called from background thread.
    /// </summary>
    /// <returns><c>true</c> if input was read; <c>false</c> otherwise</returns>
    public async Task<bool> GetInputAsync() {
        FlushNewLines().Wait();
        IsExpectingInput = true;

        var r = await this.WhenAnyValue(x => x.IsExpectingInput)
            .Merge(Observable.Interval(TimeSpan.FromMilliseconds(500)).Select(_ => false)).Where(b => !b).FirstAsync();

        Debug.WriteLine(r ? "Got input!" : "No input!");
        return r;
    }
}