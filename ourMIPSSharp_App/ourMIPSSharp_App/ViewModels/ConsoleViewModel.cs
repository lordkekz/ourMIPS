using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using lib_ourMIPSSharp.CompilerComponents.Elements;
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
            FlushNewLines();
    }

    private void TextOutWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 1;
        _newLines.Enqueue(e.Content);
        if (ShouldAutoUpdateConsole())
            FlushNewLines();
    }

    private void TextErrWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 2;
        _newLines.Enqueue(e.Content);
        if (ShouldAutoUpdateConsole())
            FlushNewLines();
    }

    private void TextInWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 3;
        _newLines.Enqueue("Input: " + e.Content);
        if (ShouldAutoUpdateConsole())
            FlushNewLines();
    }

    public bool HasNewLines => !_newLines.IsEmpty;

    /// <summary>
    /// Flushes new lines to UI. Automatically runs itself in UI Thread if needed.
    /// </summary>
    public void FlushNewLines() {
        if (!Dispatcher.UIThread.CheckAccess()) {
            // Switch to UI Thread
            Dispatcher.UIThread.InvokeAsync(FlushNewLines).Wait();
            return;
        }

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

    public void SubmitInput() {
        IsExpectingInput = false;
        Backend.TextInWriter.WriteLine(InputString);
        FlushNewLines();
        InputString = "";
    }

    public async Task<bool> GetInputAsync() {
        FlushNewLines();
        IsExpectingInput = true;
        foreach (var b in this.WhenAnyValue(x => x.IsExpectingInput).ToEnumerable())
            if (!b)
                break;

        Debug.WriteLine("Got input!");
        return true;
    }

    /// <summary>
    /// Gets input synchronously. Must be called from Background thread!
    /// </summary>
    /// <returns><c>true</c> if input was read; <c>false</c> otherwise</returns>
    /// <exception cref="InvalidOperationException">When called from UI thread</exception>
    public bool GetInput() {
        if (Dispatcher.UIThread.CheckAccess())
            throw new InvalidOperationException(
                "GetInput must not be called from UI thread! Consider using GetInputAsync.");

        var t = GetInputAsync();
        t.Wait();
        return t.Result;
    }
}