﻿using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class StatementSequenceNode : AstNode
    {
        public IEnumerable<AstNode> SubNodes
        {
            get
            {
                return subNodes;
            }
        }

        List<AstNode> subNodes;

        public StatementSequenceNode()
        {
            subNodes = new List<AstNode>();
        }

        public StatementSequenceNode(IEnumerable<AstNode> subNodes)
        {
            this.subNodes.AddRange(subNodes);
        }

        public void AddStatement(AstNode node)
        {
            subNodes.Add(node);
        }

        public override void EmitCode(CodeEmitter emitter)
        {
            emitter.Emit(OpCode.Nop);
        }
    }
}
