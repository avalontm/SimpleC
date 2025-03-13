using System.Diagnostics;
using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para llamar a una función
    /// </summary>
    public static class CallFunction
    {
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null) return;

            // Guardar posición inicial
            int startIp = vm.Ip;

            // Avanzar después del opcode Call
            vm.Ip++;

            try
            {
                // Leer el nombre de la función
                byte nameLength = vm.Bytecode[vm.Ip++];
                byte[] nameBytes = new byte[nameLength];
                for (int i = 0; i < nameLength; i++)
                {
                    nameBytes[i] = vm.Bytecode[vm.Ip++];
                }
                string functionName = Encoding.UTF8.GetString(nameBytes);

                // Leer número de argumentos
                byte argCount = vm.Bytecode[vm.Ip++];

                // Verificar que hay suficientes valores en la pila
                if (vm.Stack.Count < argCount)
                {
                    vm.ReportError($"Stack underflow: Not enough arguments for function {functionName}");
                    return;
                }

                // Recolectar argumentos
                List<object> args = new List<object>();
                for (int i = 0; i < argCount; i++)
                {
                    object arg = vm.Stack.Pop();
                    args.Insert(0, arg); // Invertir el orden
                }

                vm.OnDebugMessage($"Calling function '{functionName}' with arguments: {string.Join(", ", args)}");

                // Buscar la función en la tabla de funciones
                if (vm.FunctionTable.TryGetValue(functionName, out int functionPosition))
                {
                    if (functionPosition < 0)
                    {
                        // Función nativa
                        if (functionName == "printf" || functionName == "print")
                        {
                            ExecutePrintFunction(args);
                        }
                        else if (functionName == "scanf" || functionName == "input")
                        {
                            ExecuteScanfFunction(args);
                        }
                        else
                        {
                            vm.ReportError($"Unknown native function: {functionName}");
                        }
                    }
                    else
                    {
                        // Función definida por el usuario
                        vm.OnDebugMessage($"Executing user function at position {functionPosition}");

                        // Guardar dirección de retorno
                        vm.CallStack.Push(vm.Ip);

                        // Crear un nuevo contexto para la función
                        ExecutionContext functionContext = new ExecutionContext(functionName);

                        // Extraer nombres de parámetros
                        List<string> paramNames = ExtractParameterNames(vm, functionPosition);

                        // Asignar argumentos a parámetros en el contexto
                        for (int i = 0; i < paramNames.Count && i < args.Count; i++)
                        {
                            functionContext.Variables[paramNames[i]] = args[i];
                            vm.OnDebugMessage($"Assigned parameter {paramNames[i]} = {args[i]}");
                        }

                        // Agregar el contexto a la pila de contextos
                        vm.LocalContexts.Push(functionContext);

                        // Saltar al cuerpo de la función
                        vm.Ip = functionPosition;
                        SkipToFunctionBody(vm);
                    }
                }
                else
                {
                    vm.ReportError($"Function not found: {functionName}");
                }
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error in CallFunction: {ex.Message}");
                vm.Ip = startIp + 1; // Asegurar que avanzamos
            }
        }

        private static List<string> ExtractParameterNames(VirtualMachine vm, int functionPosition)
        {
            List<string> paramNames = new List<string>();
            int savedIp = vm.Ip;

            try
            {
                // Validar que la posición de la función está dentro del bytecode
                if (functionPosition < 0 || functionPosition >= vm.Bytecode.Count)
                {
                    vm.OnDebugMessage($"Invalid function position: {functionPosition}");
                    return paramNames;
                }

                // Ir a la posición de la función
                vm.Ip = functionPosition;

                // Verificar que hay suficiente espacio para leer
                if (!EnsureRemainingBytes(vm, 3)) // Mark, nombre de función, número de parámetros
                {
                    vm.OnDebugMessage("Not enough bytes to read function metadata");
                    return paramNames;
                }

                // Avanzar después del Mark
                vm.Ip++;

                // Leer longitud del nombre de la función
                byte nameLength = vm.Bytecode[vm.Ip++];

                // Verificar que hay suficiente espacio para el nombre de la función
                if (!EnsureRemainingBytes(vm, nameLength + 1))
                {
                    vm.OnDebugMessage("Not enough bytes to read function name");
                    return paramNames;
                }

                // Saltar el nombre de la función
                vm.Ip += nameLength;

                // Leer número de parámetros
                byte paramCount = vm.Bytecode[vm.Ip++];

                // Leer nombres de parámetros
                for (int i = 0; i < paramCount; i++)
                {
                    // Verificar que hay suficiente espacio para leer la longitud del nombre del parámetro
                    if (!EnsureRemainingBytes(vm, 1))
                    {
                        vm.OnDebugMessage($"Not enough bytes to read parameter {i} length");
                        break;
                    }

                    byte paramNameLength = vm.Bytecode[vm.Ip++];

                    // Verificar que hay suficiente espacio para el nombre del parámetro
                    if (!EnsureRemainingBytes(vm, paramNameLength))
                    {
                        vm.OnDebugMessage($"Not enough bytes to read parameter {i} name");
                        break;
                    }

                    // Crear un array seguro para el nombre del parámetro
                    byte[] paramNameBytes = new byte[paramNameLength];

                    // Copiar los bytes del nombre del parámetro de forma segura
                    Array.Copy(vm.Bytecode.ToArray(), vm.Ip, paramNameBytes, 0, paramNameLength);

                    // Convertir a string
                    string paramName = Encoding.UTF8.GetString(paramNameBytes);
                    paramNames.Add(paramName);

                    // Avanzar más allá del nombre del parámetro
                    vm.Ip += paramNameLength;
                }

                vm.OnDebugMessage($"Extracted {paramNames.Count} parameter names");
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error extracting parameter names: {ex.Message}");
            }
            finally
            {
                // Restaurar IP original
                vm.Ip = savedIp;
            }

            return paramNames;
        }

        private static bool EnsureRemainingBytes(VirtualMachine vm, int requiredBytes)
        {
            return vm.Ip + requiredBytes <= vm.Bytecode.Count;
        }

        // Método para saltar al cuerpo de la función (después del Enter)
        private static void SkipToFunctionBody(VirtualMachine vm)
        {
            // Buscar el Enter que marca el inicio del cuerpo de la función
            while (vm.Ip < vm.Bytecode.Count)
            {
                if ((OpCode)vm.Bytecode[vm.Ip] == OpCode.Enter)
                {
                    vm.Ip++; // Avanzar después del Enter
                    vm.OnDebugMessage($"Function body starts at position {vm.Ip}");
                    return;
                }
                vm.Ip++;
            }
        }

        private static void ExecutePrintFunction(List<object> args)
        {
            var vm = VirtualMachine.Instance;

            try
            {
                if (args.Count > 0)
                {
                    string output = args[0]?.ToString() ?? "null";
                    Console.WriteLine(output);
                    vm.OnDebugMessage($"Printf: {output}");

                    // Poner un valor en la pila como resultado
                    vm.Stack.Push(output.Length);
                }
                else
                {
                    Console.WriteLine();
                    vm.OnDebugMessage("Printf: (empty line)");
                    vm.Stack.Push(0);
                }
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error in printf: {ex.Message}");
                vm.Stack.Push(0);
            }
        }

        private static void ExecuteScanfFunction(List<object> args)
        {
            var vm = VirtualMachine.Instance;
            if (vm == null) return;

            try
            {
                if (args.Count >= 1)
                {
                    string prompt = "";

                    // Si hay un primer parámetro, usarlo como prompt
                    if (args.Count >= 2)
                    {
                        prompt = args[1]?.ToString() ?? "";
                        Console.Write(prompt);
                    }

                    // Leer la entrada del usuario
                    string input = Console.ReadLine() ?? "";

                    // El primer parámetro debe ser el nombre de la variable donde guardar el valor
                    if (args[0] is string varName)
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
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error in Scanf: {ex.Message}");
                vm.Stack.Push("");
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