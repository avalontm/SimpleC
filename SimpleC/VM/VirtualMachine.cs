using SimpleC.Utils;
using SimpleC.VM.Instructions;
using System.Diagnostics;
using System.Text;

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
        public bool MainFound { get; set; } = false;

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

            // Registrar funciones nativas
            RegisterBuiltInFunctions();

            // Primer paso: escanear todo el bytecode para registrar variables globales y encontrar funciones
            ScanBytecode();

            // Segundo paso: inicializar valores de variables globales
            InitGlobalVariables();

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

            // Imprimir información de las funciones registradas para debugging
            OnDebugMessage("--- Registered Functions ---");
            foreach (var kvp in FunctionTable)
            {
                OnDebugMessage($"Function: {kvp.Key} at position {kvp.Value}");
            }
            OnDebugMessage("------------------------");
        }

        private void RegisterBuiltInFunctions()
        {
            // Registrar la función printf
            if (!FunctionTable.ContainsKey("printf"))
            {
                FunctionTable["printf"] = -1; // -1 indica función nativa
                OnDebugMessage("Registered built-in function: printf");
            }

            // Registrar la función print (alias de printf)
            if (!FunctionTable.ContainsKey("print"))
            {
                FunctionTable["print"] = -1;
                OnDebugMessage("Registered built-in function: print");
            }

            // Registrar la función scanf
            if (!FunctionTable.ContainsKey("scanf"))
            {
                FunctionTable["scanf"] = -2; // -2 indica función nativa scanf
                OnDebugMessage("Registered built-in function: scanf");
            }

            // Registrar la función input (alias de scanf)
            if (!FunctionTable.ContainsKey("input"))
            {
                FunctionTable["input"] = -2;
                OnDebugMessage("Registered built-in function: input");
            }

            // Aquí puedes registrar otras funciones nativas si es necesario
        }

        public void DebugBytecode()
        {
            Console.WriteLine("\n===== BYTECODE DEBUG =====");

            for (int i = 0; i < Bytecode.Count; i++)
            {
                byte currentByte = Bytecode[i];

                // Mostrar posición y valor en hexadecimal
                Console.Write($"[{i:D4}] 0x{currentByte:X2}");

                // Convertir byte a int antes de verificar si es un opcode válido
                int opcodeValue = currentByte;
                if (currentByte.IsValidOpCode())
                {
                    OpCode opcode = (OpCode)opcodeValue;
                    Console.Write($" - {opcode}");

                    // Para opcodes específicos, mostrar detalles adicionales
                    if (opcode == OpCode.Mark)
                    {
                        Console.Write(" (Function definition)");

                        // Intentar leer el nombre de la función
                        if (i + 1 < Bytecode.Count)
                        {
                            byte nameLength = Bytecode[i + 1];
                            if (i + 2 + nameLength <= Bytecode.Count)
                            {
                                byte[] nameBytes = new byte[nameLength];
                                for (int j = 0; j < nameLength; j++)
                                {
                                    nameBytes[j] = Bytecode[i + 2 + j];
                                }
                                string funcName = System.Text.Encoding.UTF8.GetString(nameBytes);
                                Console.Write($" - Function: '{funcName}'");
                            }
                        }
                    }
                    else if (opcode == OpCode.Call)
                    {
                        Console.Write(" (Function call)");

                        // Intentar leer el nombre de la función llamada
                        if (i + 1 < Bytecode.Count)
                        {
                            byte nameLength = Bytecode[i + 1];
                            if (i + 2 + nameLength <= Bytecode.Count)
                            {
                                byte[] nameBytes = new byte[nameLength];
                                for (int j = 0; j < nameLength; j++)
                                {
                                    nameBytes[j] = Bytecode[i + 2 + j];
                                }
                                string funcName = System.Text.Encoding.UTF8.GetString(nameBytes);
                                Console.Write($" - Calling: '{funcName}'");
                            }
                        }
                    }
                }

                Console.WriteLine();
            }

            Console.WriteLine("=========================\n");
        }

        /// <summary>
        /// Inicializa los valores de las variables globales
        /// </summary>
        private void InitGlobalVariables()
        {
            int savedIp = Ip;
            Ip = 0;

            OnDebugMessage("Initializing global variables...");

            try
            {
                // Ejecutar todas las instrucciones hasta encontrar la primera función (Mark)
                while (Ip < Bytecode.Count)
                {
                    if (Ip < Bytecode.Count && Bytecode[Ip] == (byte)OpCode.Mark)
                        break;

                    byte opcode = Bytecode[Ip];
                    CurrentOpcode = opcode;
                    OnDebugMessage($"Global init - Executing opcode: {(OpCode)opcode} at position {Ip}");

                    try
                    {
                        // Ejecutar las instrucciones para inicializar variables globales
                        switch ((OpCode)opcode)
                        {
                            case OpCode.LoadC:
                                LoadConstant.Execute();
                                OnDebugMessage($"Loaded constant to stack, stack count: {Stack.Count}");
                                if (Stack.Count > 0)
                                    OnDebugMessage($"Stack top value: {Stack.Peek()}");
                                break;

                            case OpCode.StoreGlobal:
                                StoreVariable.Execute();
                                break;

                            default:
                                Ip++; // Avanzar para otros opcodes
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnDebugMessage($"Error executing opcode {(OpCode)opcode}: {ex.Message}");
                        Ip++; // Avanzar para evitar bucle infinito
                    }
                }

                // Información de diagnóstico: mostrar todas las variables globales y sus valores
                OnDebugMessage("--- Global Variables after initialization ---");
                foreach (var kvp in GlobalContext.Variables)
                {
                    OnDebugMessage($"Global: {kvp.Key} = {kvp.Value ?? "null"}");
                }
                OnDebugMessage("----------------------------------------");
            }
            catch (Exception ex)
            {
                OnDebugMessage($"Error in global variables initialization: {ex.Message}");
            }
            finally
            {
                // Limpiar la pila después de inicializar variables globales
                Stack.Clear();
                Ip = savedIp; // Restaurar posición
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
                    if (Ip >= Bytecode.Count) break;

                    byte opcode = Bytecode[Ip];
                    OpCode operation = (OpCode)opcode;

                    if (operation == OpCode.Mark)
                    {
                        // Guardar la posición del Mark
                        int markPosition = Ip;

                        // Avanzar después del Mark
                        Ip++;

                        // Leer longitud del nombre
                        if (Ip >= Bytecode.Count) break;
                        byte nameLength = Bytecode[Ip++];

                        if (nameLength > 0 && Ip + nameLength <= Bytecode.Count)
                        {
                            // Leer el nombre de la función
                            byte[] nameBytes = new byte[nameLength];
                            for (int i = 0; i < nameLength; i++)
                            {
                                nameBytes[i] = Bytecode[Ip + i];
                            }
                            string functionName = Encoding.UTF8.GetString(nameBytes);

                            // Registrar la función
                            OnDebugMessage($"Registering function: {functionName} at position {markPosition}");
                            FunctionTable[functionName] = markPosition;

                            // Especialmente si es main, marcar que la encontramos
                            if (functionName == MAIN_FUNCTION_NAME)
                            {
                                MainFound = true;
                                OnDebugMessage("Main function found!");
                            }
                        }

                        // Avanzar después del nombre
                        Ip += nameLength;
                    }

                    // Avanzar al siguiente byte
                    Ip++;
                }
            }
            catch (Exception ex)
            {
                OnDebugMessage($"Error in ScanBytecode: {ex.Message}");
            }
            finally
            {
                // Restaurar la posición original
                Ip = savedIp;
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
        // Modificar el método Run() en VirtualMachine.cs
        public void Run()
        {
            try
            {
                // Forzar a buscar main explícitamente
                if (!MainFound)
                {
                    // Buscar explícitamente en el bytecode
                    for (int i = 0; i < Bytecode.Count - 4; i++)
                    {
                        if ((OpCode)Bytecode[i] == OpCode.Mark &&
                            i + 1 < Bytecode.Count &&
                            Bytecode[i + 1] == 4 && // Longitud del nombre "main"
                            i + 5 < Bytecode.Count)
                        {
                            // Verificar si es "main"
                            byte[] nameBytes = new byte[4];
                            for (int j = 0; j < 4; j++)
                            {
                                nameBytes[j] = Bytecode[i + 2 + j];
                            }
                            string name = Encoding.UTF8.GetString(nameBytes);

                            if (name == "main")
                            {
                                MainFound = true;
                                FunctionTable["main"] = i;
                                OnDebugMessage($"Forced main function detection at position {i}");
                                break;
                            }
                        }
                    }
                }

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
        /// Encuentra y ejecuta una función registrada por su nombre
        /// </summary>
        /// <param name="functionName">Nombre de la función a ejecutar</param>
        public void FindAndExecuteRegisteredFunction(string functionName)
        {
            if (FunctionTable.TryGetValue(functionName, out int functionPosition))
            {
                OnDebugMessage($"Executing registered function: {functionName} at position {functionPosition}");

                // Guardar la posición actual del IP
                int savedIp = Ip;

                // Establecer el IP a la posición de la función
                Ip = functionPosition;

                // Crear un nuevo contexto local para la función
                LocalContexts.Push(new ExecutionContext(functionName));

                try
                {
                    // Ejecutar instrucciones de la función
                    ExecuteInstructions();
                }
                finally
                {
                    // Restaurar el contexto previo
                    if (LocalContexts.Count > 0)
                        LocalContexts.Pop();

                    // Restaurar la posición original del IP
                    Ip = savedIp;
                }
            }
            else
            {
                OnDebugMessage($"Function not found: {functionName}");
            }
        }

        /// <summary>
        /// Bucle principal de ejecución de instrucciones
        /// </summary>
        // Modifica el método ExecuteInstructions en VirtualMachine.cs

        private void ExecuteInstructions()
        {
            // Agregar un límite de instrucciones para evitar bucles infinitos
            int maxInstructions = 10000; // Límite razonable
            int instructionCount = 0;

            int lastIp = -1; // Para detectar si el IP no avanza
            int stuckCounter = 0; // Contador para detectar cuándo estamos atascados

            while (Ip < Bytecode.Count && instructionCount < maxInstructions)
            {
                // Detectar si estamos atascados en la misma instrucción
                if (Ip == lastIp)
                {
                    stuckCounter++;
                    if (stuckCounter > 5) // Si estamos atascados por más de 5 ciclos
                    {
                        OnDebugMessage($"Posible bucle infinito detectado en IP {Ip}, avanzando manualmente");
                        Ip++; // Avanzar manualmente
                        stuckCounter = 0;
                        continue;
                    }
                }
                else
                {
                    stuckCounter = 0;
                    lastIp = Ip;
                }

                byte opcode = Bytecode[Ip];
                CurrentOpcode = opcode; // Actualizar variable de clase para uso en operaciones
                OnDebugMessage($"Executing opcode: {(OpCode)opcode} at position {Ip}");

                try
                {
                    // Guardar posición actual del IP
                    int currentIp = Ip;

                    // Simplemente delegamos la ejecución a las clases especializadas en cada instrucción
                    switch ((OpCode)opcode)
                    {
                        case OpCode.LoadC: // 0x10 - Cargar constante
                            LoadConstant.Execute();
                            break;

                        case OpCode.Load: // 0x11 - Cargar variable
                            LoadVariable.Execute();
                            break;

                        case OpCode.LoadGlobal: // Cargar variable global
                            LoadVariable.Execute();
                            break;

                        case OpCode.Store: // 0x12 - Almacenar variable
                            StoreVariable.Execute();
                            break;

                        case OpCode.StoreGlobal: // Almacenar variable global
                            StoreVariable.Execute();
                            break;

                        case OpCode.Dup: // 0x13 - Duplicar valor superior
                            DuplicateValue.Execute();
                            break;

                        case OpCode.Pop: // 0x20 - Extraer valor
                            PopValue.Execute();
                            break;

                        case OpCode.Add: // 0x30 - Suma o concatenación
                            ArithmeticOperations.ExecuteAdd();
                            break;

                        case OpCode.Sub: // 0x31 - Resta
                            ArithmeticOperations.ExecuteSubtract();
                            break;

                        case OpCode.Mul: // 0x32 - Multiplicación
                            ArithmeticOperations.ExecuteMultiply();
                            break;

                        case OpCode.Div: // 0x33 - División
                            ArithmeticOperations.ExecuteDivide();
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

                        case OpCode.JumpIfFalse:
                            JumpInstructions.ExecuteJumpIfFalse();
                            break;

                        case OpCode.Jump:
                            JumpInstructions.ExecuteJump();
                            break;

                        default:
                            OnDebugMessage($"Unknown opcode: {opcode} (0x{opcode:X2}) at position {Ip}");
                            Ip++; // Avanzar manualmente en caso de opcode desconocido
                            break;
                    }

                    // Verificar si la instrucción no avanzó el IP
                    if (Ip == currentIp)
                    {
                        OnDebugMessage($"Instrucción no avanzó el IP, avanzando manualmente: {(OpCode)opcode}");
                        Ip++; // Avanzar manualmente
                    }
                }
                catch (Exception ex)
                {
                    OnDebugMessage($"Error ejecutando opcode {(OpCode)opcode}: {ex.Message}");
                    // Avanzar en caso de error para evitar bucles infinitos
                    Ip++;
                }

                instructionCount++;
            }

            if (instructionCount >= maxInstructions)
            {
                OnDebugMessage($"Posible bucle infinito detectado - se alcanzó el límite de {maxInstructions} instrucciones");
            }
        }
    }
}