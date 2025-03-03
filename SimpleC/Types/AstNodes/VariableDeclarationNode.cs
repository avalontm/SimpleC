using SimpleC.CodeGeneration;
using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    class VariableDeclarationNode : AstNode
    {
        public ExpressionNode InitialValueExpression { get; private set; }
        public VariableType Type { get; private set; }

        public string Name { get; private set; }

        private static readonly ExpressionNode DefaultIntValueExpression = ExpressionNode.CreateConstantExpression(0); // El valor predeterminado para un entero (int) es cero (0).

        /// <summary>
        /// Crea una nueva instancia de la clase VariableDeclarationNode.
        /// </summary>
        /// <param name="type">El tipo de la variable.</param>
        /// <param name="name">El nombre de la variable.</param>
        /// <param name="initialValue">Una expresión utilizada para inicializar la variable o null para usar el valor predeterminado.</param>
        public VariableDeclarationNode(VariableType type, string name, ExpressionNode initialValue)
        {
            Type = type;
            Name = name;

            initialValue = initialValue ?? DefaultIntValueExpression;
            InitialValueExpression = initialValue;
        }

        public override void EmitCode(CodeEmitter emitter)
        {

        }
    }
}
