using SimpleC.Parsing;

namespace SimpleC.Types.AstNodes
{
    class WhileLoopNode : LoopStatementNode
    {
        public ExpressionNode Condition { get; private set; }

        public WhileLoopNode(ExpressionNode cond)
        {
            if (cond == null)
                throw new ParsingException("Analizador: ¡Un bucle 'while' debe contener una condición!");
        }
    }
}
