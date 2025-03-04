using SimpleC.Types.AstNodes;
using System.Diagnostics;

namespace SimpleC.Parsing
{
    public static class ParserGlobal
    {
        static Dictionary<string, StatementSequenceNode> Global = new Dictionary<string, StatementSequenceNode>();

        public static void Register(string key, StatementSequenceNode node)
        {
            if (Verify(key))
            {
                return;
            }
            Debug.WriteLine($"Global Register: {key}");
            Global.Add(key, node);
        }

        public static bool Verify(string key)
        {
            return Global.ContainsKey(key);
        }

        public static StatementSequenceNode? Get(string key)
        {
            if (!Verify(key))
            {
                return null;
            }
            return Global[key];
        }

    }
}
