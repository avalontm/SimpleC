﻿using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class StatementSequenceNode : AstNode
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

    }
}
