namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para extraer el valor superior de la pila
    /// </summary>
    public static class PopValue
    {
        /// <summary>
        /// Ejecuta la instrucción Pop que extrae el valor superior de la pila
        /// </summary>
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;

            if (vm.Stack.Count > 0)
            {
                var value = vm.Stack.Pop();
                vm.OnDebugMessage($"Value popped: {value}");
            }
            else
            {
                vm.ReportError("Cannot pop: stack is empty", false);
            }

            vm.Ip++;
        }
    }
}