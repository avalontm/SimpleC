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
                Debug.WriteLine($"methodName: {methodName}" + $" {vm.FunctionTable.ContainsKey(methodName)}");
                // Verificar si es una función definida por el usuario o integrada
                if (vm.FunctionTable.ContainsKey(methodName))
                {
                   
                    // Usar el nuevo método para ejecutar funciones registradas
                    vm.FindAndExecuteRegisteredFunction(methodName);
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

                    case "scanf":
                    case "input":
                        if (parameters.Count >= 1)
                        {
                            string prompt = "";

                            // Si hay un primer parámetro, usarlo como prompt
                            if (parameters.Count >= 2)
                            {
                                prompt = parameters[1]?.ToString() ?? "";
                                Console.Write(prompt);
                            }

                            // Leer la entrada del usuario
                            string input = Console.ReadLine() ?? "";

                            // El primer parámetro debe ser el nombre de la variable donde guardar el valor
                            if (parameters[0] is string varName)
                            {
                                // Buscar primero en variables locales
                                bool found = false;

                                // Verificar contexto local
                                if (vm.LocalContexts.Count > 0)
                                {
                                    var localContext = vm.LocalContexts.Peek();
                                    if (localContext.Variables.ContainsKey(varName))
                                    {
                                        // Determinar el tipo de la variable
                                        object currentValue = localContext.Variables[varName];

                                        // Convertir la entrada al tipo correcto
                                        object newValue = ConvertInputToType(input, currentValue);

                                        // Guardar el valor en la variable
                                        localContext.Variables[varName] = newValue;
                                        found = true;
                                        vm.OnDebugMessage($"Scanf stored '{newValue}' in local variable '{varName}'");
                                    }
                                }

                                // Si no se encontró en local, buscar en global
                                if (!found && vm.GlobalContext.Variables.ContainsKey(varName))
                                {
                                    // Determinar el tipo de la variable
                                    object currentValue = vm.GlobalContext.Variables[varName];

                                    // Convertir la entrada al tipo correcto
                                    object newValue = ConvertInputToType(input, currentValue);

                                    // Guardar el valor en la variable
                                    vm.GlobalContext.Variables[varName] = newValue;
                                    found = true;
                                    vm.OnDebugMessage($"Scanf stored '{newValue}' in global variable '{varName}'");
                                }

                                if (!found)
                                {
                                    vm.OnDebugMessage($"Scanf could not find variable '{varName}' to store input");
                                }
                            }
                            else
                            {
                                vm.OnDebugMessage("Scanf first parameter must be a variable name");
                            }
                        }
                        else
                        {
                            vm.OnDebugMessage("Scanf requires at least 1 parameter for the variable name");
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

        // Añadir este método auxiliar para convertir la entrada del usuario al tipo correcto
        private static object ConvertInputToType(string input, object targetValue)
        {
            if (targetValue is int)
            {
                if (int.TryParse(input, out int intValue))
                    return intValue;
                return 0;
            }
            else if (targetValue is float)
            {
                if (float.TryParse(input, out float floatValue))
                    return floatValue;
                return 0.0f;
            }
            else if (targetValue is bool)
            {
                if (bool.TryParse(input, out bool boolValue))
                    return boolValue;

                // Casos adicionales
                input = input.ToLower().Trim();
                if (input == "1" || input == "yes" || input == "y" || input == "si" || input == "s")
                    return true;

                return false;
            }
            else if (targetValue is char)
            {
                if (input.Length > 0)
                    return input[0];
                return '\0';
            }
            else
            {
                // Para strings y otros tipos, devolver la entrada sin cambios
                return input;
            }
        }
    }
}