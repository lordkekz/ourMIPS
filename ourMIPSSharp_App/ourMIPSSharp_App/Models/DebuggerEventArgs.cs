using System;

namespace ourMIPSSharp_App.Models;

public class DebuggerBreakEventHandlerArgs : EventArgs {
    public int Address { get; }
    public int Line { get; }

    public DebuggerBreakEventHandlerArgs(int address, int line) {
        Address = address;
        Line = line;
    }

}

public class DebuggerUpdatingEventHandlerArgs : EventArgs {
    /// <summary>
    /// Defines whether changes during this event should be highlighted.
    /// Should be false if update was not because of instruction execution.
    /// </summary>
    public bool RaisesChangeHighlight { get; }

    public DebuggerUpdatingEventHandlerArgs(bool raisesChangeHighlight) {
        RaisesChangeHighlight = raisesChangeHighlight;
    }
}