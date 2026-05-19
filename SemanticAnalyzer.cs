using System;
using System.Collections.Generic;

namespace lab_666
{
    public class SemanticError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
    }

    public class SemanticAnalyzer
    {
        private List<SemanticError> _errors = new List<SemanticError>();
        private HashSet<string> _variables = new HashSet<string>();
        private int _tempCount = 0;
        private List<Quadruple> _quads = new List<Quadruple>();

        public List<SemanticError> Errors => _errors;

        private string NewTemp()
        {
            return $"t{++_tempCount}";
        }

        private void AddQuad(string op, string arg1, string arg2, string result)
        {
            _quads.Add(new Quadruple(op, arg1 ?? "", arg2 ?? "", result));
        }

        public List<Quadruple> GetQuads() => _quads;

        public AstNode Analyze(AstNode root)
        {
            _errors.Clear();
            _variables.Clear();
            _tempCount = 0;
            _quads.Clear();

            if (root == null) return null;

            if (root is BlockNode block)
            {
                foreach (var stmt in block.Statements)
                {
                    AnalyzeStatement(stmt);
                }
            }
            else
            {
                // ЛЮБОЕ выражение, не являющееся блоком, анализируем
                AnalyzeExpression(root);
            }

            return root;
        }

        private void AnalyzeStatement(AstNode stmt)
        {
            if (stmt is AssignNode assign)
            {
                _variables.Add(assign.Identifier);
                var result = AnalyzeExpression(assign.Expression);
                AddQuad("=", result, "", assign.Identifier);
            }
            else if (stmt is BinaryOpNode binary)
            {
                // Обработка выражения как оператора
                AnalyzeExpression(binary);
            }
            else if (stmt is NumberNode num)
            {
                // Число само по себе - не создаем тетраду
            }
            else if (stmt is IdentifierNode id)
            {
                // Идентификатор сам по себе
                if (!_variables.Contains(id.Name) && _variables.Count > 0)
                {
                    _errors.Add(new SemanticError
                    {
                        Line = id.Line,
                        Column = id.Column,
                        Message = $"Необъявленная переменная: {id.Name}"
                    });
                }
            }
        }

        private string AnalyzeExpression(AstNode expr)
        {
            if (expr is NumberNode num)
            {
                return num.Value.ToString();
            }
            else if (expr is IdentifierNode id)
            {
                if (!_variables.Contains(id.Name) && _variables.Count > 0)
                {
                    _errors.Add(new SemanticError
                    {
                        Line = id.Line,
                        Column = id.Column,
                        Message = $"Необъявленная переменная: {id.Name}"
                    });
                }
                return id.Name;
            }
            else if (expr is BinaryOpNode binary)
            {
                var left = AnalyzeExpression(binary.Left);
                var right = AnalyzeExpression(binary.Right);
                var temp = NewTemp();
                AddQuad(binary.Operator, left, right, temp);
                return temp;
            }

            return "";
        }
    }
}