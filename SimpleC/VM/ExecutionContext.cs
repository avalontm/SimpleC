using System.Collections.Generic;

namespace SimpleC.VM
{
    /// <summary>
    /// Representa un contexto de ejecución que almacena variables y su ámbito
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// Diccionario de variables en este contexto
        /// </summary>
        public Dictionary<string, object> Variables { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Nombre identificativo del contexto (ej: "global", "main", etc.)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Indica si este contexto es global
        /// </summary>
        public bool IsGlobal { get; }
        public string LastVariable { get; internal set; }

        /// <summary>
        /// Crea un nuevo contexto de ejecución
        /// </summary>
        /// <param name="name">Nombre del contexto</param>
        /// <param name="isGlobal">Indica si es un contexto global</param>
        public ExecutionContext(string name, bool isGlobal = false)
        {
            Name = name;
            IsGlobal = isGlobal;
        }

        internal void SetVariable(string varName)
        {
            LastVariable = varName;
        }
    }
}