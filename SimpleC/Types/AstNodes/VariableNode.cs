using LLVMSharp.Interop;
using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class VariableNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        public string Name { get; }
        public string Operator { get; }
        public string Value { get; }
        bool Root { get; }

        public VariableNode(VariableType type, Token name, Token oper, List<Token> tokens, bool root)
        {
            Root = root;
            Debug.WriteLine($"Operator: {root}");
            Debug.WriteLine($"VariableType: {type}");
            Debug.WriteLine($"Name: {name}");
            Debug.WriteLine($"Operator: {oper.Content}");

            if (type != VariableType.Int && type != VariableType.Float && type != VariableType.Char && type != VariableType.String && type != VariableType.Bool)
            {
                throw new Exception($"Se esperar un tipo de variable ");
            }

            if(oper.Content != "=")
            {
                throw new Exception($"Se esperaba un operador '=': en la línea {oper.Line}, posición {oper.Column}");
            }

            var token = tokens.GetEnumerator();

            while (token.MoveNext())
            {
                if (!token.Current.Content.Contains(";"))
                {
                    Value += $"{token.Current.Content} ";
                }
            }

            Type = type;
            Name = name.Content;
            Operator = oper.Content;

            ParserGlobal.Register(Name, this);

            Console.WriteLine(this.ToString());
            Generate();
        }

        public override void Generate()
        {
            LLVMSharp.Variable(Type, Name, Value, Root);
        }


        public override string ToString()
        {
            return $"{Type} {Name} {Operator} {Value}";
        }
    }
}
