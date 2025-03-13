using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para llamar a una función
    /// </summary>
    public static class CallFunction
    {
        /// <summary>
        /// Ejecuta la instrucción Call que llama a una función
        /// </summary>
        public static void Execute()
        {
            Debug.WriteLine($"CallFunction");
            VirtualMachine.Instance.Ip++; // Avanzar más allá del opcode CALL

            // Leer longitud del nombre del método
            if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
                VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading method name length");

            byte methodNameLength = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip++];

            // Leer nombre del método
            if (VirtualMachine.Instance.Ip + methodNameLength > VirtualMachine.Instance.Bytecode.Count)
                VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading method name");

            // Crear array para leer los bytes del nombre
            byte[] nameBytes = new byte[methodNameLength];
            for (int i = 0; i < methodNameLength; i++)
            {
                nameBytes[i] = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip++];
            }
            string methodName = Encoding.UTF8.GetString(nameBytes);

            VirtualMachine.Instance.OnDebugMessage($"Calling method: {methodName}");

            // Leer cantidad de argumentos
            if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
                VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading argument count");

            byte argCount = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip++];
            VirtualMachine.Instance.OnDebugMessage($"Method has {argCount} arguments");

            // Todos los argumentos deberían estar ahora en la pila en orden inverso
            // Recolectarlos en orden correcto
            List<object> args = new List<object>();
            for (int i = 0; i < argCount; i++)
            {
                if (VirtualMachine.Instance.Stack.Count > 0)
                {
                    object arg = VirtualMachine.Instance.Stack.Pop();
                    args.Insert(0, arg); // Insertar al principio para invertir orden
                }
                else
                {
                    VirtualMachine.Instance.ReportError($"Not enough values on stack for method call {methodName}");
                }
            }

            // Si es una función definida por el usuario, guardar dirección de retorno y saltar
            if (VirtualMachine.Instance.FunctionTable.TryGetValue(methodName, out int functionPosition))
            {
                // Guardar posición actual como dirección de retorno
                VirtualMachine.Instance.CallStack.Push(VirtualMachine.Instance.Ip);

                // Crear nuevo contexto de variables locales para esta función
                VirtualMachine.Instance.LocalContexts.Push(new ExecutionContext(methodName));

                // Volver a colocar argumentos en la pila para que la función los acceda
                foreach (var arg in args)
                {
                    VirtualMachine.Instance.Stack.Push(arg);
                }

                // Saltar a la posición de la función
                VirtualMachine.Instance.Ip = functionPosition;
                VirtualMachine.Instance.OnDebugMessage($"Jumping to user-defined function at position {functionPosition}");
            }
            else
            {
                // Ejecutar método integrado
                ExecuteBuiltInMethod(methodName, args);
            }
        }

        /// <summary>
        /// Ejecuta un método integrado
        /// </summary>
        /// <param name="methodName">Nombre del método</param>
        /// <param name="parameters">Lista de parámetros</param>
        private static void ExecuteBuiltInMethod(string methodName, List<object> parameters)
        {
            VirtualMachine.Instance.OnDebugMessage($"Executing built-in method: {methodName} with {parameters.Count} parameters");

            switch (methodName)
            {
                case "printf":
                    if (parameters.Count >= 1)
                    {
                        string output = parameters[0]?.ToString() ?? "null";
                        System.Console.WriteLine(output);
                    }
                    else
                    {
                        VirtualMachine.Instance.ReportError($"Printf requires at least 1 parameter, received {parameters.Count}");
                    }
                    break;

                default:
                    VirtualMachine.Instance.ReportError($"Unknown method: {methodName}");
                    break;
            }
        }
    }
}