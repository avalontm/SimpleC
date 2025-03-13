namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para retornar de una función
    /// </summary>
    public static class ProcessReturn
    {
        /// <summary>
        /// Ejecuta la instrucción Return que retorna de una función
        /// </summary>
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;

            // Verificar si tenemos un valor para retornar
            object returnValue = null;
            if (vm.Stack.Count > 0)
            {
                returnValue = vm.Stack.Peek(); // Mantener valor de retorno en la pila
                vm.OnDebugMessage($"Returning value: {returnValue}");
            }

            if (vm.CallStack.Count > 0)
            {
                // Eliminar contexto de variables locales para esta función
                if (vm.LocalContexts.Count > 0)
                {
                    var removedContext = vm.LocalContexts.Pop();
                    vm.OnDebugMessage($"Context '{removedContext.Name}' removed with {removedContext.Variables.Count} variables");
                }

                // Obtener dirección de retorno de la pila de llamadas
                int returnAddress = vm.CallStack.Pop();
                vm.OnDebugMessage($"Return: Going back to address {returnAddress}");

                // Restaurar puntero de instrucción a dirección de retorno
                vm.Ip = returnAddress;
            }
            else
            {
                // Si no hay direcciones de retorno, probablemente estamos en la función principal
                vm.OnDebugMessage("Return: No return address (possibly in main function)");
                vm.Ip++;
            }
        }
    }
}