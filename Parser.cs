using System;
using System.Collections.Generic;
using static lab_666.Lexer;

namespace lab_666
{
    public class SyntaxError
    {
        public int Line { get; set; }
        public int Position { get; set; }
        public string Description { get; set; }
    }

    public class Quadruple
    {
        public string Op { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Result { get; set; }

        public Quadruple(string op, string arg1, string arg2, string result)
        {
            Op = op;
            Arg1 = arg1;
            Arg2 = arg2;
            Result = result;
        }
    }

    public class Parser
    {
        private List<Token> _tokens;
        private int _position;
        private List<SyntaxError> _errors;
        private int _tempCount;
        private List<Quadruple> _quads;

        public List<SyntaxError> Errors => _errors;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _errors = new List<SyntaxError>();
            _tempCount = 0;
            _quads = new List<Quadruple>();
        }

        public List<Quadruple> GetQuads() => _quads;

        private Token Current => _position < _tokens.Count ? _tokens[_position] : null;

        private void AddError(string message)
        {
            var token = Current;
            _errors.Add(new SyntaxError
            {
                Line = token?.Line ?? 0,
                Position = token?.Position ?? 0,
                Description = message
            });
        }

        private bool Match(string expectedType)
        {
            if (Current != null && Current.Type == expectedType)
            {
                _position++;
                return true;
            }
            return false;
        }

        private string NewTemp()
        {
            return $"t{++_tempCount}";
        }

        private void AddQuad(string op, string arg1, string arg2, string result)
        {
            _quads.Add(new Quadruple(op, arg1, arg2, result));
        }

        public AstNode Parse()
        {
            if (_tokens.Count == 0) return null;

            var block = new BlockNode();

            while (Current != null)
            {
                var stmt = ParseStatement();
                if (stmt != null)
                    block.Statements.Add(stmt);

                if (Current != null && Current.Type == "SEMICOLON")
                    Match("SEMICOLON");
            }

            return block;
        }

        private AstNode ParseStatement()
        {
            if (Current?.Type == "ID")
            {
                var idToken = Current;
                Match("ID");

                if (Match("="))
                {
                    var expr = ParseExpression();
                    return new AssignNode
                    {
                        Identifier = idToken.Value,
                        Expression = expr,
                        Line = idToken.Line,
                        Column = idToken.Position
                    };
                }
            }

            if (Current != null)
            {
                return ParseExpression();
            }

            return null;
        }

        public AstNode ParseExpression()
        {
            return ParseE();
        }

        private AstNode ParseE()
        {
            var left = ParseT();
            return ParseA(left);
        }

        private AstNode ParseA(AstNode left)
        {
            if (Current?.Type == "+")
            {
                Match("+");
                var right = ParseT();
                var result = new BinaryOpNode
                {
                    Operator = "+",
                    Left = left,
                    Right = right
                };
                return ParseA(result);
            }
            else if (Current?.Type == "-")
            {
                Match("-");
                var right = ParseT();
                var result = new BinaryOpNode
                {
                    Operator = "-",
                    Left = left,
                    Right = right
                };
                return ParseA(result);
            }
            return left;
        }

        private AstNode ParseT()
        {
            var left = ParseF();
            return ParseB(left);
        }

        private AstNode ParseB(AstNode left)
        {
            string op = null;

            if (Current?.Type == "*") { op = "*"; Match("*"); }
            else if (Current?.Type == "/") { op = "/"; Match("/"); }
            else if (Current?.Type == "FDIV") { op = "//"; Match("FDIV"); }
            else if (Current?.Type == "%") { op = "%"; Match("%"); }
            else if (Current?.Type == "POW") { op = "**"; Match("POW"); }

            if (op != null)
            {
                var right = ParseF();
                var result = new BinaryOpNode
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
                return ParseB(result);
            }

            return left;
        }

        private AstNode ParseF()
        {
            if (Current == null)
            {
                AddError("Неожиданный конец выражения");
                return null;
            }

            if (Current.Type == "NUM")
            {
                var token = Current;
                Match("NUM");
                return new NumberNode
                {
                    Value = int.Parse(token.Value),
                    Line = token.Line,
                    Column = token.Position
                };
            }
            else if (Current.Type == "ID")
            {
                var token = Current;
                Match("ID");
                return new IdentifierNode
                {
                    Name = token.Value,
                    Line = token.Line,
                    Column = token.Position
                };
            }
            else if (Current.Type == "(")
            {
                Match("(");
                var expr = ParseE();

                if (Current == null)
                {
                    AddError("Неожиданный конец выражения, ожидалась ')'");
                    return expr;
                }

                if (Current.Type == ")")
                {
                    Match(")");
                    return expr;
                }
                else
                {
                    AddError($"Ожидалась ')', найдено: {Current.Type}");
                    // Не пропускаем токен, просто возвращаем выражение
                    return expr;
                }
            }
            else if (Current.Type == ")")
            {
                // ЛИШНЯЯ ЗАКРЫВАЮЩАЯ СКОБКА - ОБРАБОТКА
                var token = Current;
                AddError($"Лишняя закрывающая скобка");
                Match(")"); // Пропускаем лишнюю скобку
                return ParseF(); // Пробуем разобрать следующий токен
            }

            AddError($"Ожидалось число, идентификатор или '(', найдено: {Current.Type}");

            // Пропускаем проблемный токен
            if (Current != null)
            {
                _position++;
            }

            return null;
        }
    }
}