using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class MethodNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        public string Name { get; }
        public List<string> Parameters { get; } = new List<string>();
        public string Separator { get; }

        public MethodNode(VariableType type, string name, List<Token> tokens )
        {
            var token = tokens.GetEnumerator();
            token.MoveNext();

            Type = type;
            Name = name;

            if (type != VariableType.Void)
            {
                Debug.WriteLine($"Name: {Name} | {token.Current}");
            }
          
            if (token.Current?.Content != "(")
            {
                throw new Exception($"Se esperaba un '(': en la línea {token.Current.Line}, posición {token.Current.Column}");
            }

            //leer si tiene parametros

            while (token.MoveNext() && token.Current?.Content != ")")
            {
                Parameters.Add(token.Current.Content);
            }

            if (token.Current != null && token.Current?.Content != ")")
            {
                throw new Exception($"Se esperaba un ')': en la línea {token.Current.Line}, posición {token.Current.Column}");
            }

            token.MoveNext();

            if (token.Current != null && token.Current.Content == ";")
            {
                Separator = token.Current.Content;
            }

            while(token.MoveNext())
            {

            }

            ParserGlobal.Register(Name, this);

            Console.WriteLine(this.ToString());
        }

        public override string ToString()
        {
            return $"{Type} {Name}({string.Join(" ", Parameters)}){Separator}";
        }
    }
}
