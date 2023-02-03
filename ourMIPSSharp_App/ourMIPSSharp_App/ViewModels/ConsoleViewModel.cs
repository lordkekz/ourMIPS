using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class ConsoleViewModel : ViewModelBase {
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
        var str = _newLines.Aggregate("", (a, b) => a + b);
        _newLines.Clear();
        Document.Insert(Document.TextLength, str);
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
    /// <exception cref="InvalidOperationException">When called from UI thread</exception>
    public bool GetInput() {
        FlushNewLines().Wait();
        IsExpectingInput = true;
        var noLongerExpectingInput = this.WhenAnyValue(x => x.IsExpectingInput)
            .Where(b => !b).FirstAsync().ToTask();
        var r = noLongerExpectingInput.Wait(200);
        Debug.WriteLine(r ? "Got input!" : "No input!");
        return r;
    }
}