using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleC.VM
{
    /// <summary>
    /// Representa un error ocurrido durante la ejecución de la máquina virtual
    /// </summary>
    public class VMError
    {
        /// <summary>
        /// Mensaje descriptivo del error
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Posición del puntero de instrucción donde ocurrió el error
        /// </summary>
        public int InstructionPointer { get; }

        /// <summary>
        /// Contenido de la pila en el momento del error
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Momento en que ocurrió el error
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Opcode que se estaba ejecutando cuando ocurrió el error (si está disponible)
        /// </summary>
        public OpCode? Opcode { get; }

        /// <summary>
        /// Crea una nueva instancia de VMError
        /// </summary>
        /// <param name="message">Mensaje de error</param>
        /// <param name="ip">Posición del puntero de instrucción</param>
        /// <param name="stack">Pila de valores en el momento del error</param>
        /// <param name="opcode">Opcode que se estaba ejecutando (opcional)</param>
        public VMError(string message, int ip, Stack<object> stack, OpCode? opcode = null)
        {
            Message = message;
            InstructionPointer = ip;
            StackTrace = string.Join(", ", stack.Reverse());
            Timestamp = DateTime.Now;
            Opcode = opcode;
        }

        /// <summary>
        /// Convierte el error a una representación en texto
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"VM Error at {Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"  Message: {Message}");
            sb.AppendLine($"  Instruction Pointer: {InstructionPointer}");
            if (Opcode.HasValue)
            {
                sb.AppendLine($"  Opcode: {Opcode.Value} (0x{(byte)Opcode.Value:X2})");
            }
            sb.AppendLine($"  Stack: {StackTrace}");
            return sb.ToString();
        }
    }
}