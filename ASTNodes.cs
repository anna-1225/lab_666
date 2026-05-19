using System.Collections.Generic;
using System.Text;

namespace lab_666
{
    public abstract class AstNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public abstract string ToTree(int indent = 0);
    }

    public class BlockNode : AstNode
    {
        public List<AstNode> Statements { get; set; } = new List<AstNode>();

        public override string ToTree(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(' ', indent * 2);
            sb.AppendLine("Block:");
            foreach (var stmt in Statements)
            {
                sb.Append(stmt.ToTree(indent + 1));
            }
            return sb.ToString();
        }
    }

    public class AssignNode : AstNode
    {
        public string Identifier { get; set; }
        public AstNode Expression { get; set; }

        public override string ToTree(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(' ', indent * 2);
            sb.AppendLine($"Assign: {Identifier} =");
            sb.Append(Expression.ToTree(indent + 1));
            return sb.ToString();
        }
    }

    public class BinaryOpNode : AstNode
    {
        public string Operator { get; set; }
        public AstNode Left { get; set; }
        public AstNode Right { get; set; }

        public override string ToTree(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(' ', indent * 2);
            sb.AppendLine($"BinaryOp: {Operator}");
            sb.Append(Left.ToTree(indent + 1));
            sb.Append(Right.ToTree(indent + 1));
            return sb.ToString();
        }
    }

    public class NumberNode : AstNode
    {
        public int Value { get; set; }

        public override string ToTree(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(' ', indent * 2);
            sb.AppendLine($"Number: {Value}");
            return sb.ToString();
        }
    }

    public class IdentifierNode : AstNode
    {
        public string Name { get; set; }

        public override string ToTree(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(' ', indent * 2);
            sb.AppendLine($"Identifier: {Name}");
            return sb.ToString();
        }
    }
}