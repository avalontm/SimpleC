using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class StatementSequenceNode : AstNode
    {
        private Dictionary<string, VariableType> LocalVariables { get; } = new();

        public void Register(string name, VariableType type)
        {
            if (LocalVariables.ContainsKey(name))
            {
                throw new Exception($"La Variable '{name}' ({type}) ya se ha registrado.");
            }
            LocalVariables[name] = type;
            Debug.WriteLine($"Variable local registrada: {name} ({type})");
        }


        public bool Verify(string name)
        {
            return LocalVariables.ContainsKey(name);
        }

        public VariableType? Get(string key)
        {
            if (!Verify(key))
            {
                return null;
            }
            return LocalVariables[key];
        }

        public VariableType? GetType(string name)
        {
            if (LocalVariables.TryGetValue(name, out var type))
            {
                return type;
            }
            return null;
        }

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
