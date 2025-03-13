using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para entrar a un bloque de función
    /// </summary>
    public static class ProcessEnter
    {
        /// <summary>
        /// Ejecuta la instrucción Enter que procesa la entrada a un bloque de función
        /// </summary>
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            vm.OnDebugMessage("Enter function block");

            // Crear nuevo contexto local antes de procesar parámetros
            string contextName = $"Function_Context_{vm.LocalContexts.Count + 1}";
            vm.LocalContexts.Push(new ExecutionContext(contextName));
            vm.OnDebugMessage($"Created new local context: {contextName}");

            // Leer tipo de retorno y avanzar
            vm.Ip++;
            if (vm.Ip < vm.Bytecode.Count)
            {
                byte returnType = vm.Bytecode[vm.Ip];
                vm.OnDebugMessage($"Enter: Return type: {(ConstantType)returnType}");
                vm.Ip++;

                // Leer número de parámetros
                if (vm.Ip < vm.Bytecode.Count)
                {
                    byte paramCount = vm.Bytecode[vm.Ip];
                    vm.OnDebugMessage($"Enter: Parameter count: {paramCount}");
                    vm.Ip++;

                    // Obtener el contexto de función actual (recién creado)
                    ExecutionContext currentContext = vm.LocalContexts.Peek();

                    // Procesar parámetros - ya deberían estar en la pila desde la llamada
                    List<object> paramValues = new List<object>();
                    if (vm.Stack.Count >= paramCount)
                    {
                        for (int i = 0; i < paramCount; i++)
                        {
                            paramValues.Insert(0, vm.Stack.Pop()); // Orden inverso
                        }
                    }

                    // Procesar cada parámetro
                    int paramIndex = 0;
                    for (int i = 0; i < paramCount; i++)
                    {
                        // Leer tipo de parámetro
                        if (vm.Ip < vm.Bytecode.Count)
                        {
                            byte paramType = vm.Bytecode[vm.Ip];
                            vm.Ip++;

                            // Leer longitud del nombre del parámetro
                            if (vm.Ip < vm.Bytecode.Count)
                            {
                                byte paramNameLength = vm.Bytecode[vm.Ip];
                                vm.Ip++;

                                // Leer nombre del parámetro
                                if (vm.Ip + paramNameLength <= vm.Bytecode.Count)
                                {
                                    string paramName = Encoding.UTF8.GetString(vm.Bytecode.ToArray(), vm.Ip, paramNameLength);
                                    vm.OnDebugMessage($"Enter: Parameter: {(ConstantType)paramType} {paramName}");

                                    // Almacenar valor del parámetro en variables locales si está disponible
                                    if (paramIndex < paramValues.Count)
                                    {
                                        currentContext.Variables[paramName] = paramValues[paramIndex];
                                        vm.OnVariableChanged(paramName, paramValues[paramIndex]);
                                        vm.OnDebugMessage($"Parameter {paramName} set to {paramValues[paramIndex++]}");
                                    }

                                    vm.Ip += paramNameLength;
                                }
                                else
                                {
                                    vm.OnDebugMessage("Error reading parameter name");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}