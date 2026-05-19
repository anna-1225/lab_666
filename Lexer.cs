using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace lab_666
{
    public class LexerError
    {
        public int Line { get; set; }
        public int Position { get; set; }
        public string Description { get; set; }
    }

    public class Token
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }

        public Token(string type, string value, int line, int position)
        {
            Type = type;
            Value = value;
            Line = line;
            Position = position;
        }

        public override string ToString()
        {
            return $"({Type}, '{Value}', {Line}:{Position})";
        }
    }

    public static class Lexer
    {
        private static List<LexerError> _errors = new List<LexerError>();

        public static List<Token> Tokenize(string input)
        {
            _errors.Clear();
            var tokens = new List<Token>();
            int pos = 0;
            int line = 1;
            int lineStart = 0;

            while (pos < input.Length)
            {
                char c = input[pos];

                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n')
                    {
                        line++;
                        lineStart = pos + 1;
                    }
                    pos++;
                    continue;
                }

                if (char.IsDigit(c))
                {
                    string num = "";
                    int startPos = pos;
                    while (pos < input.Length && char.IsDigit(input[pos]))
                        num += input[pos++];
                    tokens.Add(new Token("NUM", num, line, startPos - lineStart + 1));
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    string id = "";
                    int startPos = pos;
                    while (pos < input.Length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_'))
                        id += input[pos++];
                    tokens.Add(new Token("ID", id, line, startPos - lineStart + 1));
                }
                else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '(' || c == ')' || c == '=')
                {
                    int startPos = pos;
                    if (c == '*' && pos + 1 < input.Length && input[pos + 1] == '*')
                    {
                        tokens.Add(new Token("POW", "**", line, startPos - lineStart + 1));
                        pos += 2;
                    }
                    else if (c == '/' && pos + 1 < input.Length && input[pos + 1] == '/')
                    {
                        tokens.Add(new Token("FDIV", "//", line, startPos - lineStart + 1));
                        pos += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(c.ToString(), c.ToString(), line, startPos - lineStart + 1));
                        pos++;
                    }
                }
                else
                {
                    _errors.Add(new LexerError
                    {
                        Line = line,
                        Position = pos - lineStart + 1,
                        Description = $"Недопустимый символ '{c}'"
                    });
                    pos++;
                }
            }

            return tokens;
        }

        public static List<LexerError> GetErrors()
        {
            return _errors;
        }
    }
}