using SimpleC.Utils;
using SimpleC.VM.Instructions;
using System.Diagnostics;

namespace SimpleC.VM
{
    /// <summary>
    /// Clase principal de la máquina virtual que ejecuta bytecode
    /// </summary>
    public class VirtualMachine
    {
        // Propiedades públicas para acceso desde instrucciones
        public List<byte> Bytecode { get; private set; }
        public int Ip { get; set; } // Puntero de instrucción
        public Stack<object> Stack { get; private set; } // Pila de valores para diferentes tipos de datos

        // Contexto de ejecución para variables
        public ExecutionContext GlobalContext { get; private set; }
        public Stack<ExecutionContext> LocalContexts { get; private set; }

        public ExecutionContext CurrentContext => LocalContexts.Count > 0 ? LocalContexts.Peek() : GlobalContext;

        public Dictionary<string, int> FunctionTable { get; private set; } // Mapeo nombre de función -> posición IP
        public Stack<int> CallStack { get; private set; } // Pila para direcciones de retorno

        // Lista para almacenar errores
        private List<VMError> _errors;
        public IReadOnlyList<VMError> Errors => _errors.AsReadOnly();

        // Constante para la función principal
        private const string MAIN_FUNCTION_NAME = "main";
        public bool MainFound { get; private set; } = false;

        // Opcode actual para operaciones que lo necesitan
        public byte CurrentOpcode { get; set; } = 0;

        // Patrón Singleton para acceso global
        public static VirtualMachine? Instance { get; private set; }

        /// <summary>
        /// Constructor de la máquina virtual
        /// </summary>
        /// <param name="bytecode">Lista de bytes que contiene el programa a ejecutar</param>
        public VirtualMachine(List<byte> bytecode)
        {
            Bytecode = bytecode;
            Ip = 0;
            Stack = new Stack<object>();

            // Inicializamos con contextos en lugar de diccionarios
            GlobalContext = new ExecutionContext("global", true);
            LocalContexts = new Stack<ExecutionContext>();

            FunctionTable = new Dictionary<string, int>();
            CallStack = new Stack<int>();
            _errors = new List<VMError>();

            // Establecer la instancia única
            Instance = this;

            // Inicializar la máquina virtual
            Init();
        }


        public void OnDebugMessage(string message)
        {
            Debug.WriteLine(message);
        }

        public void OnVariableChanged(string message, object value)
        {
            Debug.WriteLine($"{message} {value}");
        }

        /// <summary>
        /// Método para reportar errores 
        /// </summary>
        /// <param name="message">Mensaje de error</param>
        /// <param name="throwException">Indica si se debe lanzar una excepción después de reportar</param>
        public void ReportError(string message, bool throwException = true)
        {
            var error = new VMError(message, Ip, Stack, CurrentOpcode == 0 ? null : (OpCode)CurrentOpcode);
            _errors.Add(error);

            OnDebugMessage(error.ToString());

            if (throwException)
            {
                throw new Exception($"VM Error: {message} at position {Ip}");
            }
        }

        /// <summary>
        /// Inicializa la máquina virtual: carga variables globales y encuentra funciones
        /// </summary>
        private void Init()
        {
            OnDebugMessage("Initializing virtual machine...");

            // Primer paso: escanear todo el bytecode para cargar variables globales y encontrar funciones
            ScanBytecode();

            // Verificar si se encontró la función principal
            MainFound = FunctionTable.ContainsKey(MAIN_FUNCTION_NAME);

            if (!MainFound)
            {
                OnDebugMessage($"Warning: Main function '{MAIN_FUNCTION_NAME}' not found");
            }
            else
            {
                OnDebugMessage($"Main function found at position {FunctionTable[MAIN_FUNCTION_NAME]}");
            }
        }

        /// <summary>
        /// Escanea todo el bytecode para encontrar variables globales y funciones
        /// </summary>
        private void ScanBytecode()
        {
            int savedIp = Ip;
            Ip = 0;

            try
            {
                while (Ip < Bytecode.Count)
                {
                    byte opcode = Bytecode[Ip];
                    OpCode operation = (OpCode)opcode;
                    Debug.WriteLine($"ScanBytecode: {operation}");

                    switch (operation)
                    {
                        case OpCode.Mark:
                            // Encontrado el inicio de una función
                            RegisterFunction.Execute();
                            break;

                        case OpCode.Load:
                            // Registrar variable global
                            LoadVariable.Execute();
                            break;
                        case OpCode.Store:
                            // Registrar variable global
                            StoreVariable.Execute();
                            break;
                        default:
                            // Avanzar al siguiente byte
                            Ip++;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error scanning bytecode: {ex.Message}", false);
            }
            finally
            {
                Ip = savedIp; // Restaurar posición
            }
        }

        /// <summary>
        /// Devuelve un valor predeterminado para un tipo dado
        /// </summary>
        /// <param name="type">Tipo de constante</param>
        /// <returns>Valor predeterminado para el tipo</returns>
        public object GetDefaultValueForType(ConstantType type)
        {
            switch (type)
            {
                case ConstantType.Integer: return 0;
                case ConstantType.Float: return 0.0f;
                case ConstantType.String: return "";
                case ConstantType.Char: return '\0';
                case ConstantType.Bool: return false;
                case ConstantType.Void: return null;
                default: return null;
            }
        }

        /// <summary>
        /// Inicia la ejecución del programa
        /// </summary>
        public void Run()
        {
            try
            {
                if (!MainFound)
                {
                    ReportError("Execution halted: main function not found", false);
                    return;
                }

                // Ejecutar función principal
                ExecuteMainFunction();
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (_errors.Count == 0 || _errors.Last().Message != errorMessage)
                {
                    ReportError(errorMessage, false);
                }

#if DEBUG
                ColorParser.WriteLine($"[color=red]Execution error: {ex}[/color]");
#else
                ColorParser.WriteLine($"[color=red]Execution error: {ex.Message}[/color]");
#endif
            }
        }

        /// <summary>
        /// Ejecuta la función principal
        /// </summary>
        private void ExecuteMainFunction()
        {
            if (FunctionTable.TryGetValue(MAIN_FUNCTION_NAME, out int mainPosition))
            {
                OnDebugMessage($"Executing main function '{MAIN_FUNCTION_NAME}' from position {mainPosition}...");

                Ip = mainPosition;

                // Inicializar contexto local para la función principal
                LocalContexts.Push(new ExecutionContext(MAIN_FUNCTION_NAME));

                ExecuteInstructions();
            }
            else
            {
                ReportError($"Main function '{MAIN_FUNCTION_NAME}' not found");
            }
        }

        /// <summary>
        /// Bucle principal de ejecución de instrucciones
        /// </summary>
        private void ExecuteInstructions()
        {
            while (Ip < Bytecode.Count)
            {
                byte opcode = Bytecode[Ip];
                CurrentOpcode = opcode; // Actualizar variable de clase para uso en operaciones
                OnDebugMessage($"Executing opcode: {(OpCode)opcode} at position {Ip}");

                try
                {
                    // Simplemente delegamos la ejecución a las clases especializadas en cada instrucción
                    switch ((OpCode)opcode)
                    {
                        case OpCode.LoadC: // 0x10 - Cargar constante
                            LoadConstant.Execute();
                            break;

                        case OpCode.Load: // 0x11 - Cargar variable
                            LoadVariable.Execute();
                            break;

                        case OpCode.Store: // 0x12 - Almacenar variable
                            StoreVariable.Execute();
                            break;

                        case OpCode.Dup: // 0x13 - Duplicar valor superior
                            DuplicateValue.Execute();
                            break;

                        case OpCode.Pop: // 0x20 - Extraer valor
                            PopValue.Execute();
                            break;

                        case OpCode.Mark: // 0x80 - Marca de definición de función
                            ProcessMark.Execute();
                            break;

                        case OpCode.LoadS: // 0x15 - Cargar string
                            LoadString.Execute();
                            break;

                        case OpCode.Call: // 0x60 - Llamar función
                            CallFunction.Execute();
                            break;

                        case OpCode.Enter: // 0x81 - Entrar a bloque de función
                            ProcessEnter.Execute();
                            break;

                        case OpCode.Return: // 0x85 - Retornar de función
                            ProcessReturn.Execute();
                            break;

                        case OpCode.Halt: // 0xFF - Finalizar programa
                            OnDebugMessage("Program execution halted");
                            return;

                        default:
                            ReportError($"Unknown opcode: {opcode} (0x{opcode:X2}) at position {Ip}");
 
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ReportError($"Error executing opcode {(OpCode)opcode}: {ex.Message}", true);
                }
            }
        }
    }
}