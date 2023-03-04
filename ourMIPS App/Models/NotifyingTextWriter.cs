using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ourMIPSSharp_App.Models;

public class NotifyingTextWriter : TextWriter {
    public override Encoding Encoding { get; } = Encoding.Default;
    private readonly StringBuilder _sb = new();
    private int _lineStartIndex = 0;

    public event EventHandler<NotifyingTextWriterEventArgs>? CharWritten;
    public event EventHandler<NotifyingTextWriterEventArgs>? LineWritten;

    protected virtual void OnCharWritten(NotifyingTextWriterEventArgs e) {
        CharWritten?.Invoke(this, e);
    }

    protected virtual void OnLineWritten(NotifyingTextWriterEventArgs e) {
        LineWritten?.Invoke(this, e);
    }

    public override void Write(char value) {
        _sb.Append(value);
        OnCharWritten(new NotifyingTextWriterEventArgs(value.ToString()));
        if (value != '\n') return;
        
        OnLineWritten(new NotifyingTextWriterEventArgs(
            _sb.ToString(_lineStartIndex, _sb.Length - _lineStartIndex)));
        _lineStartIndex = _sb.Length;
    }

    public override string ToString() => _sb.ToString();
}

public class NotifyingTextWriterEventArgs : EventArgs {
    public string Content { get; private set; }

    public NotifyingTextWriterEventArgs(string content) {
        Content = content;
    }
}