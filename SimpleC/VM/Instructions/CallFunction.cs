using System;
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
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            Debug.WriteLine($"CallFunction");
            int startIp = vm.Ip;
            vm.Ip++; // Avanzar más allá del opcode CALL

            try
            {
                // Leer longitud del nombre del método
                if (vm.Ip >= vm.Bytecode.Count)
                {
                    vm.OnDebugMessage("Unexpected end of bytecode while reading method name length");
                    vm.Ip = startIp + 1; // Asegurar que avanzamos
                    return;
                }

                byte methodNameLength = vm.Bytecode[vm.Ip++];

                // Leer nombre del método
                if (vm.Ip + methodNameLength > vm.Bytecode.Count)
                {
                    vm.OnDebugMessage("Unexpected end of bytecode while reading method name");
                    vm.Ip = startIp + 1; // Asegurar que avanzamos
                    return;
                }

                // Crear array para leer los bytes del nombre
                byte[] nameBytes = new byte[methodNameLength];
                for (int i = 0; i < methodNameLength; i++)
                {
                    nameBytes[i] = vm.Bytecode[vm.Ip++];
                }
                string methodName = Encoding.UTF8.GetString(nameBytes);

                vm.OnDebugMessage($"Calling method: {methodName}");

                // Leer cantidad de argumentos
                if (vm.Ip >= vm.Bytecode.Count)
                {
                    vm.OnDebugMessage("Unexpected end of bytecode while reading argument count");
                    vm.Ip = startIp + 1; // Asegurar que avanzamos
                    return;
                }

                byte argCount = vm.Bytecode[vm.Ip++];
                vm.OnDebugMessage($"Method has {argCount} arguments");

                // Verificar que hay suficientes valores en la pila
                if (vm.Stack.Count < argCount)
                {
                    vm.OnDebugMessage($"Stack underflow: Not enough values on stack for method call {methodName}. Expected {argCount}, found {vm.Stack.Count}");
                    // Continuar con los argumentos que hay
                    argCount = (byte)Math.Min(argCount, vm.Stack.Count);
                }

                // Recolectar argumentos en orden correcto
                List<object> args = new List<object>();
                for (int i = 0; i < argCount; i++)
                {
                    object arg = vm.Stack.Pop();
                    args.Insert(0, arg); // Insertar al principio para invertir orden
                    vm.OnDebugMessage($"Argument {i}: {arg ?? "null"}");
                }

                // Si es una función definida por el usuario, guardar dirección de retorno y saltar
                if (vm.FunctionTable.TryGetValue(methodName, out int functionPosition))
                {
                    // Guardar posición actual como dirección de retorno
                    vm.CallStack.Push(vm.Ip);

                    // Crear nuevo contexto de variables locales para esta función
                    vm.LocalContexts.Push(new ExecutionContext(methodName));

                    // Volver a colocar argumentos en la pila para que la función los acceda
                    foreach (var arg in args)
                    {
                        vm.Stack.Push(arg);
                    }

                    // Saltar a la posición de la función
                    vm.Ip = functionPosition;
                    vm.OnDebugMessage($"Jumping to user-defined function at position {functionPosition}");
                }
                else
                {
                    // Ejecutar método integrado
                    ExecuteBuiltInMethod(methodName, args);
                }
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error executing CallFunction: {ex.Message}");
                // Asegurar que avanzamos el IP para evitar bucles
                if (vm.Ip <= startIp)
                    vm.Ip = startIp + 1;
            }
        }

        /// <summary>
        /// Ejecuta un método integrado
        /// </summary>
        /// <param name="methodName">Nombre del método</param>
        /// <param name="parameters">Lista de parámetros</param>
        private static void ExecuteBuiltInMethod(string methodName, List<object> parameters)
        {
            var vm = VirtualMachine.Instance;
            vm.OnDebugMessage($"Executing built-in method: {methodName} with {parameters.Count} parameters");

            try
            {
                switch (methodName.ToLower())
                {
                    case "printf":
                    case "print":
                        if (parameters.Count >= 1)
                        {
                            string output = parameters[0]?.ToString() ?? "null";
                            System.Console.WriteLine(output);
                        }
                        else
                        {
                            vm.OnDebugMessage($"Printf requires at least 1 parameter, received {parameters.Count}");
                            System.Console.WriteLine();
                        }
                        break;

                    case "printint":
                        if (parameters.Count >= 1)
                        {
                            if (parameters[0] is int intVal)
                            {
                                System.Console.WriteLine(intVal);
                            }
                            else if (int.TryParse(parameters[0]?.ToString(), out int parsedInt))
                            {
                                System.Console.WriteLine(parsedInt);
                            }
                            else
                            {
                                System.Console.WriteLine(parameters[0]);
                            }
                        }
                        else
                        {
                            vm.OnDebugMessage($"PrintInt requires at least 1 parameter, received {parameters.Count}");
                            System.Console.WriteLine("0");
                        }
                        break;

                    case "printfloat":
                        if (parameters.Count >= 1)
                        {
                            if (parameters[0] is float floatVal)
                            {
                                System.Console.WriteLine(floatVal);
                            }
                            else if (float.TryParse(parameters[0]?.ToString(), out float parsedFloat))
                            {
                                System.Console.WriteLine(parsedFloat);
                            }
                            else
                            {
                                System.Console.WriteLine(parameters[0]);
                            }
                        }
                        else
                        {
                            vm.OnDebugMessage($"PrintFloat requires at least 1 parameter, received {parameters.Count}");
                            System.Console.WriteLine("0.0");
                        }
                        break;

                    default:
                        vm.OnDebugMessage($"Unknown method: {methodName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error in built-in method '{methodName}': {ex.Message}");
            }
        }
    }
}