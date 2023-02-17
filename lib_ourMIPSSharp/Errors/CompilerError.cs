using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.Errors;

public class CompilerError : IComparable<CompilerError>, IEquatable<CompilerError> {
    public CompilerSeverity Severity { get; }
    public int Line { get; }
    public int Column { get; }
    public int Length { get; }
    public string? Message { get; }
    public CompilerErrorException Exception { get; }

    public CompilerError(Token t, string? message) : this(t.Line, t.Column, t.Length, message) { }

    public CompilerError(int line, int column, int length, string? message = null,
        CompilerSeverity severity = CompilerSeverity.Error) {
        Severity = severity;
        Line = line;
        Column = column;
        Length = length;
        Message = message;
        Exception = new CompilerErrorException(this);
    }

    public override string ToString() => Message != null
        ? $"[Line {Line}, Column {Column}, Len {Length}] {GetType().Name}: {Message}"
        : $"[Line {Line}, Column {Column}, Len {Length}] {GetType().Name}";

    public static implicit operator Exception(CompilerError e) => e.Exception;

    #region Comparable

    public int CompareTo(CompilerError? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var severityComparison = Severity.CompareTo(other.Severity);
        if (severityComparison != 0) return severityComparison;
        var lineComparison = Line.CompareTo(other.Line);
        if (lineComparison != 0) return lineComparison;
        var columnComparison = Column.CompareTo(other.Column);
        if (columnComparison != 0) return columnComparison;
        var lengthComparison = Length.CompareTo(other.Length);
        if (lengthComparison != 0) return lengthComparison;
        return string.Compare(Message, other.Message, StringComparison.Ordinal);
    }

    public static bool operator <(CompilerError? left, CompilerError? right) {
        return Comparer<CompilerError>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(CompilerError? left, CompilerError? right) {
        return Comparer<CompilerError>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(CompilerError? left, CompilerError? right) {
        return Comparer<CompilerError>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(CompilerError? left, CompilerError? right) {
        return Comparer<CompilerError>.Default.Compare(left, right) >= 0;
    }

    #endregion Comparable

    #region Equatable

    public bool Equals(CompilerError? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Severity == other.Severity && Line == other.Line && Column == other.Column && Length == other.Length &&
               Message == other.Message;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((CompilerError)obj);
    }

    #endregion
}

public enum CompilerSeverity {
    Error,
    Warning
}