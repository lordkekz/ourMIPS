using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    public ConsoleViewModel(OpenScriptBackend backend) {
        Backend = backend;
        Backend.TextInfoWriter.LineWritten += TextInfoWriterOnLineWritten;
        Backend.TextInfoWriter.WriteLine("Shit happens");
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

    public void FlushNewLines() {
        foreach (var line in _newLines)
            Document.Insert(Document.TextLength, line);
        _newLines.Clear();
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
}