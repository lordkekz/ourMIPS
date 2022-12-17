﻿namespace lib_ourMIPSSharp;

/// <summary>
/// Class to tokenize a source code string into a regularized list of easily-processable Tokens.
/// Detects some syntax errors such as misplaced characters or unclosed strings.
/// Doesn't validate the validity of Token contents.
/// Token lists always end with InstructionBreak Tokens, but only one InstructionBreak Token will be created in a row.
/// Comments are preserved as Comment tokens so that they may be restored while outputting modified source code.
/// </summary>
public class Tokenizer {
    private TokenizerState _state;
    private Token? _current;
    private List<Token> _result;
    private readonly string _sourcecode;
    private int _line;
    private int _col;

    public Tokenizer(string sourcecode) {
        _sourcecode = sourcecode;
        _state = TokenizerState.None;
    }

    public List<Token> Tokenize() {
        // Don't tokenize for a second time
        if (_state != TokenizerState.None)
            return null;

        // Initialize shit
        _state = TokenizerState.Whitespace;
        _current = new Token();
        _result = new List<Token>();
        _line = 1;
        _col = 1;

        // Read sourcecode
        foreach (var c in _sourcecode) {
            if (c == '\r')
                continue;
            switch (_state) {
                default:
                case TokenizerState.None:
                case TokenizerState.Whitespace:
                    if (char.IsDigit(c) || c is '+' or '-') {
                        StartToken(TokenType.Number);
                        _state = TokenizerState.InNumber;
                        AppendChar(c);
                    }
                    else if (char.IsLetter(c)) {
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
                        EndToken();
                        StartToken(TokenType.Comment);
                        _state = TokenizerState.InComment;
                    }
                    else if (char.IsWhiteSpace(c)) { }
                    else throw new SyntaxError($"Unexpected character '{c}' at line {_line}, col {_col}!");
                    break;
                
                case TokenizerState.InNumber:
                case TokenizerState.InWord:
                    if (c == '\n')
                        HandleLineBreak();
                    else if (char.IsLetterOrDigit(c) || c == '_')
                        AppendChar(c);
                    else if (char.IsWhiteSpace(c) || c == ':')
                        EndToken();
                    else if (c is ';' or '#') {
                        EndToken();
                        StartToken(TokenType.Comment);
                        _state = TokenizerState.InComment;
                    }
                    else throw new SyntaxError($"Unexpected character '{c}' at line {_line}, col {_col}!");
                    break;
                
                case TokenizerState.InString:
                    // Console.WriteLine(i.ToString() + " " + c);
                    if (c == '\\')
                        _state = TokenizerState.InStringEscaped;
                    else if (c == '"') {
                        EndToken();
                        _state = TokenizerState.Whitespace;
                    }
                    else if (c == '\n')
                        throw new SyntaxError($"Illegal line break during string literal at line {_line}, col {_col}!");
                    else
                        AppendChar(c);
                    break;
                
                case TokenizerState.InStringEscaped:
                    _state = TokenizerState.InString;
                    AppendChar(c);
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
        _col = 1;
        _line++;
    }

    private void EndToken() {
        if (_current is not null)
            _result.Add(_current);
        _current = null;
        _state = TokenizerState.Whitespace;
    }

    private void StartToken(TokenType t) {
        _current = new Token {
            Line = _line,
            Column = _col,
            Type = t
        };
    }

    private void AppendChar(char c) {
        _current.Content += c;
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
}