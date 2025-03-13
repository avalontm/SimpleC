using System.Diagnostics;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para duplicar el valor superior de la pila
    /// </summary>
    public static class DuplicateValue
    {
        /// <summary>
        /// Ejecuta la instrucción Dup que duplica el valor superior en la pila
        /// </summary>
        public static void Execute()
        {
            if (VirtualMachine.Instance.Stack.Count > 0)
            {
                var top = VirtualMachine.Instance.Stack.Peek();
                VirtualMachine.Instance.Stack.Push(top);
                VirtualMachine.Instance.OnDebugMessage($"Value duplicated: {top}");
            }
            else
            {
                VirtualMachine.Instance.ReportError("Cannot duplicate: stack is empty");
            }
            VirtualMachine.Instance.Ip++;
        }
    }
}