using System;
using System.Collections.Generic;

namespace lab_666
{
    public class RPNCalculator
    {
        private Dictionary<string, int> _precedence = new Dictionary<string, int>
        {
            { "+", 1 },
            { "-", 1 },
            { "*", 2 },
            { "/", 2 },
            { "//", 2 },
            { "%", 2 },
            { "**", 3 }
        };

        public List<string> ToRPN(string expression)
        {
            var output = new List<string>();
            var stack = new Stack<string>();

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if (char.IsWhiteSpace(c)) continue;

                if (char.IsDigit(c))
                {
                    string num = "";
                    while (i < expression.Length && char.IsDigit(expression[i]))
                        num += expression[i++];
                    output.Add(num);
                    i--;
                }
                else if (c == '(')
                {
                    stack.Push("(");
                }
                else if (c == ')')
                {
                    while (stack.Count > 0 && stack.Peek() != "(")
                        output.Add(stack.Pop());
                    if (stack.Count > 0 && stack.Peek() == "(")
                        stack.Pop();
                }
                else
                {
                    string op = c.ToString();

                    if (c == '*' && i + 1 < expression.Length && expression[i + 1] == '*')
                    {
                        op = "**";
                        i++;
                    }
                    else if (c == '/' && i + 1 < expression.Length && expression[i + 1] == '/')
                    {
                        op = "//";
                        i++;
                    }

                    while (stack.Count > 0 && stack.Peek() != "(" &&
                           _precedence[stack.Peek()] >= _precedence[op])
                    {
                        output.Add(stack.Pop());
                    }
                    stack.Push(op);
                }
            }

            while (stack.Count > 0)
                output.Add(stack.Pop());

            return output;
        }

        public int EvaluateRPN(List<string> rpn)
        {
            var stack = new Stack<int>();

            foreach (var token in rpn)
            {
                if (int.TryParse(token, out int num))
                {
                    stack.Push(num);
                }
                else
                {
                    int b = stack.Pop();
                    int a = stack.Pop();

                    switch (token)
                    {
                        case "+": stack.Push(a + b); break;
                        case "-": stack.Push(a - b); break;
                        case "*": stack.Push(a * b); break;
                        case "/": stack.Push(a / b); break;
                        case "//": stack.Push(a / b); break;
                        case "%": stack.Push(a % b); break;
                        case "**": stack.Push((int)Math.Pow(a, b)); break;
                        default: throw new Exception($"Неизвестный оператор: {token}");
                    }
                }
            }

            return stack.Pop();
        }
    }
}