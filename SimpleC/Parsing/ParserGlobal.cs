using SimpleC.Types.AstNodes;
using System.Diagnostics;

namespace SimpleC.Parsing
{
    // Esta clase es estática y maneja el registro global de nodos en el programa
    // La estamos ampliando para manejar también la detección de variables globales/locales
    public static class ParserGlobal
    {
        // Registro de nodos global existente
        private static Dictionary<string, StatementSequenceNode> Registry = new Dictionary<string, StatementSequenceNode>();

        public static bool IsTranslate { get; internal set; }

        // Método auxiliar para determinar si una variable es global basada en el Parser actual
        public static bool IsGlobalScope()
        {
            return Parser.Instance?.BracketCounter == 0;
        }

        // Registra un nodo en el registro global
        public static void Register(string name, StatementSequenceNode node)
        {
            if (Registry.ContainsKey(name))
            {
                Debug.WriteLine($"Advertencia: Redefinición de '{name}'");
                Registry[name] = node;
            }
            else
            {
                Debug.WriteLine($"Registrando globalmente: {name}");
                Registry.Add(name, node);
            }
        }

        // Verifica si existe un nodo con el nombre especificado
        public static bool Verify(string name)
        {
            return Registry.ContainsKey(name);
        }

        // Obtiene un nodo del registro global
        public static StatementSequenceNode Get(string name)
        {
            if (Verify(name))
            {
                return Registry[name];
            }

            throw new KeyNotFoundException($"No se encontró el nodo '{name}' en el registro global");
        }

        // Método para limpiar el registro (útil para reiniciar el compilador)
        public static void Clear()
        {
            Registry.Clear();
        }
    }
}