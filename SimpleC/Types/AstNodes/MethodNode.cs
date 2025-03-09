using SimpleC.Parsing;
using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    internal class MethodNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        public string Name { get; }
        public List<Token> Parameters { get; } = new List<Token>();
        public string Separator { get; }

        public MethodNode(VariableType type, string name, List<Token> tokens)
        {
            var token = tokens.GetEnumerator();
            token.MoveNext();

            Type = type;
            Name = name;

            if (token.Current?.Content != "(")
            {
                throw new Exception($"Se esperaba un '(': en la línea {token.Current.Line}, posición {token.Current.Column}");
            }

            //leer si tiene parametros

            while (token.MoveNext() && token.Current?.Content != ")")
            {
                Parameters.Add(token.Current);
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

            while (token.MoveNext())
            {

            }

            ParserGlobal.Register(Name, this);

            ColorParser.WriteLine(this.ToString());
        }


        public override string ToString()
        {
            List<string> parameters = new List<string>();

            foreach(var parameter in Parameters)
            {
                parameters.Add(ColorParser.GetTokenColor(parameter));
            }

            return $"[color=blue]{Type.ToLowerString()}[/color] [color=yellow]{Name}[/color][color=magenta]([/color]{string.Join(" ", parameters)}[color=magenta])[/color]{Separator}";
        }
    }
}
