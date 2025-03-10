using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    public class ParameterNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        // El nombre del parámetro
        public string Value { get; }

        // Constructor que recibe el tipo y el nombre del parámetro
        public ParameterNode(VariableType type, string name)
        {
            NameAst = $"Parametro: {type} {name}";
            Type = type;
            Value = name;
        }

        // Método para generar el código correspondiente al parámetro
        public override void Generate()
        {
            // Generar el código en formato de texto (puede ser ajustado según el formato de salida deseado)
            ColorParser.WriteLine($"[color=blue]{Type.ToLowerString()}[/color] [color=yellow]{Value}[/color]");
        }
    }
}
