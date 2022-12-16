﻿namespace lib_ourMIPSSharp;

public class Tokenizer {
    private TokenizerState _state;
    private Token _current;
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
            switch (_state) {
                default:
                case TokenizerState.None:
                case TokenizerState.Whitespace:
                    if (char.IsDigit(c)) {
                        StartToken();
                        _current.Type = Token.TokenType.Number;
                        _state = TokenizerState.InNumber;
                        AppendChar(c);
                    }
                    else if (char.IsLetter(c)) {
                        StartToken();
                        _current.Type = Token.TokenType.Word;
                        _state = TokenizerState.InWord;
                        AppendChar(c);
                    }
                    else if (c == '"') {
                        StartToken();
                        _current.Type = Token.TokenType.String;
                        _state = TokenizerState.InString;
                    }
                    else if (c == '\n') {
                        HandleLineBreak();
                    }
                    else if (c is ';' or '#') {
                        EndToken();
                        _state = TokenizerState.InComment;
                    }
                    else if (char.IsWhiteSpace(c)) { }
                    else throw new SyntaxError($"Unexpected character '{c}' at line {_line}, col {_col}!");
                    break;
                
                case TokenizerState.InNumber:
                case TokenizerState.InWord:
                    if (c == '\n')
                        HandleLineBreak();
                    if (char.IsLetterOrDigit(c) || c == '_')
                        AppendChar(c);
                    if (char.IsWhiteSpace(c) || c == ':')
                        EndToken();
                    else if (c is ';' or '#') {
                        EndToken();
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
                    break;
            }

            _col++;
        }
        HandleLineBreak();

        return _result;
    }

    private void HandleLineBreak() {
        EndToken();
        if (_result.Count > 0 && _result.Last().Type != Token.TokenType.InstructionBreak) {
            StartToken();
            _current.Type = Token.TokenType.InstructionBreak;
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

    private void StartToken() {
        _current = new Token {
            Line = _line,
            Column = _col
        };
    }

    private void AppendChar(char c) {
        _current.Content += c;
    }

    enum TokenizerState {
        None,
        Whitespace,
        InWord,
        InNumber,
        InString,
        InStringEscaped,
        InComment
    }
}