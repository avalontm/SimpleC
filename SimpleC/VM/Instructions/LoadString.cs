using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para cargar un string en la pila a partir de bytes
    /// </summary>
    public static class LoadString
    {
        public static void Execute()
        {
            if (VirtualMachine.Instance.Stack.Count == 0)
                VirtualMachine.Instance.ReportError("Cannot load string: stack is empty");

            int stringLength = 0;

            if (VirtualMachine.Instance.Stack.Peek() is int length)
            {
                stringLength = length;
                VirtualMachine.Instance.Stack.Pop();
            }
            else
            {
                VirtualMachine.Instance.ReportError("String length must be an integer");
            }

            VirtualMachine.Instance.OnDebugMessage($"Loading string of length {stringLength}");

            if (VirtualMachine.Instance.Stack.Count < stringLength)
                VirtualMachine.Instance.ReportError($"Not enough bytes on stack for string of length {stringLength}");

            byte[] stringBytes = new byte[stringLength];

            // Extraer bytes en orden inverso (ya que la pila es LIFO)
            for (int i = stringLength - 1; i >= 0; i--)
            {
                if (VirtualMachine.Instance.Stack.Pop() is byte b)
                {
                    stringBytes[i] = b;
                }
                else
                {
                    VirtualMachine.Instance.ReportError("Expected byte on stack for loading string");
                }
            }

            string str = Encoding.UTF8.GetString(stringBytes);
            VirtualMachine.Instance.Stack.Push(str);
            VirtualMachine.Instance.OnDebugMessage($"String loaded: \"{str}\"");

            VirtualMachine.Instance.Ip++; // Avanzar a la siguiente instrucción
        }
    }
}