#region

using lib_ourMIPSSharp.CompilerComponents.Elements;

#endregion

namespace lib_ourMIPSSharp.Errors; 

/// <summary>
/// Error to signal that a known syntax feature is unavailable due to the given DialectOptions.
/// </summary>
public class DialectSyntaxError : SyntaxError {

    /// <summary>
    /// Creates a new DialectSyntaxError.
    /// </summary>
    /// <param name="feature">Description of the unavailable syntax feature</param>
    /// <param name="t">Token that uses the feature</param>
    /// <param name="flag">Name of the Flag that forbids the feature</param>
    public DialectSyntaxError(string feature, Token t, DialectOptions flag) :
        base(t, $"{feature} is not allowed due to DialectOption {flag}.") { }
}