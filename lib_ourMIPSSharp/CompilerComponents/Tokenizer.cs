#region

using System.ComponentModel;
using System.Diagnostics;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

#endregion

namespace lib_ourMIPSSharp.CompilerComponents;

/// <summary>
/// Class to tokenize a source code string into a regularized list of easily-processable Tokens.
/// Detects some syntax errors such as misplaced characters or unclosed strings.
/// Doesn't validate the validity of Token contents.
/// Token lists always end with InstructionBreak Tokens, but only one InstructionBreak Token will be created in a row.
/// Comments are preserved as Comment tokens so that they may be restored while outputting modified source code.
/// </summary>
public class Tokenizer {
    public List<CompilerError> Errors { get; } = new();
    private DialectOptions options;
    private TokenizerState _state;
    private Token? _current;
    private List<Token> _result;
    private readonly bool _fatalErrors;
    private readonly string _sourcecode;
    private int _line;
    private int _col;
    private int _index;
    private int _startIndexOfCurrent;

    public Tokenizer(string sourcecode, DialectOptions options, bool fatalErrors) {
        if (!options.IsValid())
            throw new InvalidEnumArgumentException(nameof(options), (int)options, typeof(DialectOptions));
        this.options = options;
        _sourcecode = sourcecode;
        _state = TokenizerState.None;
        _fatalErrors = fatalErrors;
    }

    public List<Token> Tokenize() {
        // Initialize shit
        _state = TokenizerState.Whitespace;
        _current = null;
        _result = new List<Token>();
        _line = 1;
        _col = 1;

        // Read sourcecode
        for (_index = 0; _index < _sourcecode.Length; _index++) {
            var c = _sourcecode[_index];
            switch (_state) {
                default:
                case TokenizerState.None:
                case TokenizerState.Whitespace:
                    if (char.IsDigit(c) || c is '+' or '-') {
                        StartToken(TokenType.Number);
                        _state = TokenizerState.InNumber;
                        AppendChar(c);
                    }
                    else if (char.IsLetter(c) || c == '$') {
                        StartToken(TokenType.Word);
                        _state = TokenizerState.InWord;
                        AppendChar(c);
                    }
                    else if (c == '"') {
                        StartToken(TokenType.String);
                        _state = TokenizerState.InString;
                    }
                    else if (c == '\n') {
                        HandleLineBreak();
                    }
                    else if (c is ';' or '#') {
                        StartToken(TokenType.Comment);
                        _state = TokenizerState.InComment;
                    }
                    else if (char.IsWhiteSpace(c)) { }
                    else if (c is ':' or ',')
                        HandleSingleChar(c);
                    else HandleError(c, $"Illegal character '{c}'!");

                    break;

                case TokenizerState.InNumber:
                case TokenizerState.InWord:
                    if (c == '\n')
                        HandleLineBreak();
                    else if (char.IsLetterOrDigit(c) || c is '_' or '[' or ']')
                        AppendChar(c);
                    else if (char.IsWhiteSpace(c))
                        EndToken();
                    else if (c is ':' or ',')
                        HandleSingleChar(c);
                    else if (c is ';' or '#') {
                        EndToken();
                        StartToken(TokenType.Comment);
                        _state = TokenizerState.InComment;
                    }
                    else HandleError(c, $"Illegal character '{c}'!");

                    break;

                case TokenizerState.InString:
                    if (c == '\\')
                        _state = TokenizerState.InStringEscaped;
                    else if (c == '"') {
                        EndToken();
                        _state = TokenizerState.Whitespace;
                    }
                    else if (c == '\n')
                        HandleError(c, "Illegal line break during string! Maybe you meant to end it here?");
                    else
                        AppendChar(c);

                    break;

                case TokenizerState.InStringEscaped:
                    _state = TokenizerState.InString;
                    // If c is 'n', put a line break, otherwise just put c.
                    AppendChar(c == 'n' ? '\n' : c);
                    break;

                case TokenizerState.InComment:
                    if (c == '\n')
                        HandleLineBreak();
                    else AppendChar(c);
                    break;
            }

            _col++;
        }

        HandleLineBreak();

        return _result;
    }

    private void HandleLineBreak() {
        EndToken();
        if (_result.Count > 0 && _result.Last().Type != TokenType.InstructionBreak) {
            StartToken(TokenType.InstructionBreak);
            EndToken();
        }

        _col = 0;
        _line++;
    }

    private void HandleSingleChar(char c) {
        EndToken();
        StartToken(TokenType.SingleChar);
        _current.Content = c.ToString();
        EndToken();
    }

    private void EndToken() {
        if (_current is not null) {
            _result.Add(_current);
            _current.Length = int.Max(_index - _startIndexOfCurrent, 1);
        }
        _current = null;
        _state = TokenizerState.Whitespace;
    }

    private void StartToken(TokenType t) {
        _current = new Token(options) {
            Line = _line,
            Column = _col,
            Type = t
        };
        _startIndexOfCurrent = _index;
    }

    private void AppendChar(char c) {
        _current!.Content += c;
        _current.Length++;
    }

    private enum TokenizerState {
        None,
        Whitespace,
        InWord,
        InNumber,
        InString,
        InStringEscaped,
        InComment
    }

    /// <summary>
    /// Handles an Error. Attempts to guess a behavior to allow the compiler to continue working; avoiding unhelpful
    /// follow-up errors but enabling the detection of independently existing errors later in the build process.
    /// </summary>
    /// <param name="c">character that caused the error</param>
    /// <param name="s">error message string</param>
    /// <exception cref="SyntaxError">thrown if <c>_fatalErrors</c> is <c>true</c></exception>
    private void HandleError(char c, string s) {
        var err = new SyntaxError(_line, _col, 1, s);
        Errors.Add(err);
        if (_fatalErrors) throw err;
        Debug.WriteLine(err.Exception);

        // If a word contains an illegal character, put it in the token anyway, so that the word remains intact for other errors.
        if (_state == TokenizerState.InWord && _current != null)
            AppendChar(c);

        // When a string is ended by line break; end string token.
        // Makes for more realistic error scanning and thus more helpful errors.
        if (_state == TokenizerState.InString && c == '\n') HandleLineBreak();
    }
}