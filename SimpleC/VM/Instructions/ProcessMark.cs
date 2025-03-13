using System.Diagnostics;
using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para procesar una marca de función
    /// </summary>
    public static class ProcessMark
    {
        /// <summary>
        /// Ejecuta la instrucción Mark que procesa una marca de función
        /// </summary>
        public static void Execute()
        {
            VirtualMachine.Instance.Ip++; // Avanzar a longitud de nombre
            if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
            {
                VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading function name length");
                return;
            }

            byte nameLength = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip];
            VirtualMachine.Instance.Ip++; // Avanzar al nombre

            if (VirtualMachine.Instance.Ip + nameLength <= VirtualMachine.Instance.Bytecode.Count)
            {
                string functionName = Encoding.UTF8.GetString(VirtualMachine.Instance.Bytecode.ToArray(), VirtualMachine.Instance.Ip, nameLength);
                VirtualMachine.Instance.OnDebugMessage($"Mark: Function name: {functionName}");

                // Avanzar más allá del nombre
                VirtualMachine.Instance.Ip += nameLength;
            }
            else
            {
                VirtualMachine.Instance.OnDebugMessage("Error reading function name");
                VirtualMachine.Instance.Ip++;
            }
        }
    }
}