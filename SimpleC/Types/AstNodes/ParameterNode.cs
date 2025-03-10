using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    public class ParameterNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        // El nombre del parámetro
        public string Name { get; }

        // Constructor que recibe el tipo y el nombre del parámetro
        public ParameterNode(VariableType type, string name)
        {
            Type = type;
            Name = name;
            if(this.Verify(name))
            {
                throw new Exception();
            }

            this.Register(name, type);
        }

        // Método para generar el código correspondiente al parámetro
        public override void Generate()
        {
            // Generar el código en formato de texto (puede ser ajustado según el formato de salida deseado)
            ColorParser.WriteLine($"[color=blue]{Type.ToLowerString()}[/color] [color=yellow]{Name}[/color]");
        }
    }
}
