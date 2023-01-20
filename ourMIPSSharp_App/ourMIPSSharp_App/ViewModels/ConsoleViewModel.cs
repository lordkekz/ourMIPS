using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
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

    public OpenScriptBackend Backend { get; }

    private ConcurrentQueue<string> _newLines = new();

    public event EventHandler LinesFlushed;

    protected virtual void OnLinesFlushed() {
        LinesFlushed?.Invoke(this, EventArgs.Empty);
    }
    
    public ConsoleViewModel(OpenScriptBackend backend) {
        Backend = backend;
        Backend.TextInfoWriter.LineWritten += TextInfoWriterOnLineWritten;
        Backend.TextOutWriter.LineWritten += TextOutWriterOnLineWritten;
        Backend.TextErrWriter.LineWritten += TextErrWriterOnLineWritten;
        Backend.TextInWriter.LineWritten += TextInWriterOnLineWritten;
    }

    private void TextInfoWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 0;
        _newLines.Enqueue(e.Content);
    }

    private void TextOutWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 1;
        _newLines.Enqueue(e.Content);
    }

    private void TextErrWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 2;
        _newLines.Enqueue(e.Content);
    }

    private void TextInWriterOnLineWritten(object? sender, NotifyingTextWriterEventArgs e) {
        ColorHints[ColorHints.Count] = 3;
        _newLines.Enqueue("Input: " + e.Content);
    }

    public bool HasNewLines => !_newLines.IsEmpty;

    public void FlushNewLines() {
        // There is no point in using DocumentTextWriter, since it also relies on Document.Insert
        // Only call Document.Insert once, since it seems to update ui every time
        var str = _newLines.Aggregate("", (a, b) => a + b);
        _newLines.Clear();
        Document.Insert(Document.TextLength, str);
        OnLinesFlushed();
    }

    public int GetColorHint(int lineNumber) => ColorHints[lineNumber - 1];

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
        Backend.TextInWriter.WriteLine(InputString);
        FlushNewLines();
        InputString = "";
    }
}