using SimpleC.Utils;

namespace SimpleC.Types.AstNodes
{
    public class AssignmentNode : StatementSequenceNode
    {
        public Token Identifier { get; private set; }
        public List<Token> Operators { get; private set; }
        public List<Token> Values { get; private set; }

        public AssignmentNode(Token identifier, List<Token> operators, List<Token> values) : base()
        {
            NameAst = $"Asignación: {identifier.Content}";
            Identifier = identifier;
            Operators =operators;
            Values = values;
        }

        public override void Generate()
        {
            base.Generate();
            List<string> values = new List<string>();

            foreach (var value in Values)
            {
                values.Add(ColorParser.GetTokenColor(value));
            }

            ColorParser.WriteLine($"{Indentation}[color=cyan]{Identifier.Content}[/color] [color=white]{string.Join(" ", Operators.Select(x => x.Content))}[/color] {string.Join(" ", values)}");
        }
    }
}
