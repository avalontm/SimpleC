using SimpleC.Utils;
using System.Diagnostics;
using System.Text;

namespace SimpleC.VM
{
    public class VirtualMachine
    {
        private List<byte> _bytecode;
        private int _ip; // Instruction Pointer
        private Stack<object> _stack; // Value stack for different data types
        private Dictionary<string, object> _variables; // Variable storage   
        private Stack<Dictionary<string, object>> _localVariables; // Variables locales para cada contexto de función

        private Dictionary<string, int> _functionTable; // Almacena nombre de función -> posición IP
        private Stack<int> _callStack; // Pila para guardar direcciones de retorno

        // Constante para identificar la función principal
        private const string MAIN_FUNCTION_NAME = "main";
        private bool _mainFound = true;

        byte opcode = 0;

        public VirtualMachine(List<byte> bytecode)
        {
            _bytecode = bytecode;
            _ip = 0;
            _stack = new Stack<object>();
            _variables = new Dictionary<string, object>();
            _localVariables = new Stack<Dictionary<string, object>>();
            _functionTable = new Dictionary<string, int>();
            _callStack = new Stack<int>();

            // Inicializar correctamente la VM
            Init();
        }

        // Inicializa la máquina virtual: carga globales y funciones
        private void Init()
        {
            Debug.WriteLine("Inicializando máquina virtual...");

            // Primer paso: analizar todo el bytecode para cargar globales y encontrar funciones
            ScanBytecode();

            // Verificar si se encontró la función principal
            if (!_functionTable.ContainsKey(MAIN_FUNCTION_NAME))
            {
                _mainFound = false;
                return;
            }

        }

        // Escanea todo el bytecode para encontrar variables globales y funciones
        private void ScanBytecode()
        {
            int savedIp = _ip;
            _ip = 0;

            try
            {
                while (_ip < _bytecode.Count)
                {
                    opcode = _bytecode[_ip];
                    OpCode operation = (OpCode)opcode;

                    switch (operation)
                    {
                        case OpCode.Mark:
                            // Encontramos el inicio de una función
                            RegisterFunction();
                            break;

                        case OpCode.Global:
                            // Procesar variable global
                            ProcessGlobalVariable();
                            break;

                        default:
                            // Avanzar al siguiente byte
                            _ip++;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al escanear el bytecode: {ex.Message}");
            }
            finally
            {
                _ip = savedIp; // Restaurar la posición
            }
        }

        // Registra una función en la tabla de funciones
        private void RegisterFunction()
        {
            int funcStart = _ip; // Guarda la posición del Mark
            _ip++; // Mover a la longitud del nombre

            byte nameLength = _bytecode[_ip];
            _ip++; // Mover al nombre

            if (_ip + nameLength <= _bytecode.Count)
            {
                string functionName = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, nameLength);

                // Registrar la función con la posición del Mark, no después del nombre
                _functionTable[functionName] = funcStart;
                Debug.WriteLine($"Función registrada: {functionName} en posición {funcStart}");

                // Avanzar más allá del nombre para no procesar esta función ahora
                _ip += nameLength;

                // Saltamos el resto de la definición de la función para continuar el escaneo
                SkipFunctionBody();
            }
            else
            {
                Debug.WriteLine("Error al leer el nombre de la función");
                _ip++;
            }
        }

        // Procesa una variable global
        private void ProcessGlobalVariable()
        {
            _ip++; // Avanzar más allá del opcode

            // Leer tipo de variable
            if (_ip >= _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer tipo de variable global");

            byte typeCode = _bytecode[_ip++];
            ConstantType varType = (ConstantType)typeCode;

            // Leer nombre de variable
            if (_ip >= _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer longitud del nombre de variable global");

            byte nameLength = _bytecode[_ip++];

            if (_ip + nameLength > _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer nombre de variable global");

            string varName = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, nameLength);
            _ip += nameLength;

            // Leer e inicializar el valor
            // Por simplicidad, inicializamos con valores por defecto
            object defaultValue = GetDefaultValueForType(varType);
            _variables[varName] = defaultValue;

            Debug.WriteLine($"Variable global registrada: {varName} de tipo {varType} con valor por defecto {defaultValue}");
        }

        // Devuelve un valor por defecto para un tipo
        private object GetDefaultValueForType(ConstantType type)
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

        // Salta el cuerpo de una función durante el escaneo
        private void SkipFunctionBody()
        {
            // Estamos después del nombre de la función

            // Avanzar después del OpCode.Enter
            if (_ip < _bytecode.Count && (OpCode)_bytecode[_ip] == OpCode.Enter)
                _ip++;

            // Avanzar después del tipo de retorno
            if (_ip < _bytecode.Count)
                _ip++;

            // Leer el número de parámetros
            if (_ip >= _bytecode.Count)
                return;

            byte paramCount = _bytecode[_ip++];

            // Saltar los parámetros
            for (int i = 0; i < paramCount; i++)
            {
                // Saltar tipo del parámetro
                if (_ip < _bytecode.Count)
                    _ip++;

                // Saltar nombre del parámetro
                if (_ip < _bytecode.Count)
                {
                    byte paramNameLength = _bytecode[_ip++];
                    if (_ip + paramNameLength <= _bytecode.Count)
                        _ip += paramNameLength;
                }
            }

            // Ahora saltamos al siguiente Mark o hasta encontrar un Return
            int nestedLevel = 1; // Ya estamos dentro de una función

            while (_ip < _bytecode.Count)
            {
                OpCode currentOp = (OpCode)_bytecode[_ip];

                if (currentOp == OpCode.Mark || currentOp == OpCode.Enter)
                    nestedLevel++;
                else if (currentOp == OpCode.Return)
                {
                    nestedLevel--;
                    if (nestedLevel == 0)
                    {
                        _ip++; // Avanzar más allá del Return
                        break;
                    }
                }

                _ip++;
            }
        }

        // Método que inicia la ejecución del programa
        public void Run()
        {
            try
            {
                if(!_mainFound)
                {
                    return;
                }

                // Intentar ejecutar la función principal
                if (!TryExecuteMainFunction())
                {
                    // Si no se encuentra la función principal, ejecutar desde el inicio
                    ExecuteFromStart();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                ColorParser.WriteLine($"[color=red]Error de ejecución: {ex}[/color]");
#else
                ColorParser.WriteLine($"[color=red]Error de ejecución: {ex.Message}[/color]");
#endif
                Debug.WriteLine($"Error ocurrido en el puntero de instrucción: {_ip}");
                Debug.WriteLine($"Contenido de la pila en el error: {string.Join(", ", _stack.Reverse())}");
            }
        }

        // Método para intentar ejecutar la función principal
        private bool TryExecuteMainFunction()
        {
            // Verificar si existe la función principal
            if (_functionTable.TryGetValue(MAIN_FUNCTION_NAME, out int mainPosition))
            {
                Debug.WriteLine($"Ejecutando función principal '{MAIN_FUNCTION_NAME}' desde la posición {mainPosition}...");
                _ip = mainPosition;

                // Inicializar un contexto local para la función principal
                _localVariables.Push(new Dictionary<string, object>());

                ExecuteInstructions();
                return true;
            }

            Debug.WriteLine($"No se encontró la función principal '{MAIN_FUNCTION_NAME}'");
            return false;
        }

        // Método para ejecutar desde el inicio si no hay función principal
        private void ExecuteFromStart()
        {
            Debug.WriteLine("Ejecutando desde el inicio del bytecode...");
            _ip = 0;

            // Inicializar un contexto local para la ejecución desde el inicio
            _localVariables.Push(new Dictionary<string, object>());

            ExecuteInstructions();
        }

        // Método principal de ejecución de instrucciones
        private void ExecuteInstructions()
        {
            while (_ip < _bytecode.Count)
            {
                byte opcode = _bytecode[_ip];
                this.opcode = opcode; // <-- Añadir esta línea para actualizar la variable de clase
                Debug.WriteLine($"Ejecutando opcode: {(OpCode)opcode} en posición {_ip}");

                switch ((OpCode)opcode)
                {
                    case OpCode.LoadC: // 0x10 - Load Constant
                        LoadConstant();
                        break;

                    case OpCode.Load: // 0x11 - Load Variable
                        LoadVariable();
                        break;

                    case OpCode.Store: // 0x12 - Store Variable
                        StoreVariable();
                        break;

                    case OpCode.Dup: // 0x13 - Duplicate top value
                        if (_stack.Count > 0)
                        {
                            var top = _stack.Peek();
                            _stack.Push(top);
                            Debug.WriteLine($"Valor duplicado: {top}");
                        }
                        else
                        {
                            throw new Exception("No se puede duplicar: la pila está vacía");
                        }
                        _ip++;
                        break;

                    case OpCode.Pop: // 0x20 - Pop top value
                        if (_stack.Count > 0)
                        {
                            var value = _stack.Pop();
                            Debug.WriteLine($"Valor extraído: {value}");
                        }
                        else
                        {
                            throw new Exception("No se puede extraer: la pila está vacía");
                        }
                        _ip++;
                        break;

                    case OpCode.Mark: // 0x80 - Mark for function definition
                        LoadMark();
                        break;

                    case OpCode.LoadS: // 0x15 - Load String
                        LoadString();
                        break;

                    case OpCode.Call: // 0x60 - Call function
                        CallFunction();
                        break;

                    case OpCode.Enter: // 0x81 - Enter function
                        LoadBlock();
                        break;

                    case OpCode.Return: // 0x85 - Return from function
                        LoadReturn();
                        break;

                    case OpCode.Add: // Addition operation
                        PerformArithmeticOperation((a, b) => a + b, (a, b) => a + b);
                        break;

                    case OpCode.Sub: // Subtraction operation
                        PerformArithmeticOperation((a, b) => a - b, (a, b) => a - b);
                        break;

                    case OpCode.Mul: // Multiplication operation
                        PerformArithmeticOperation((a, b) => a * b, (a, b) => a * b);
                        break;

                    case OpCode.Div: // Division operation
                        PerformArithmeticOperation((a, b) => a / b, (a, b) => a / b);
                        break;

                    case OpCode.Halt: // 0xFF - End program
                        Debug.WriteLine("Ejecución del programa detenida");
                        return;
                    case OpCode.Global:
                        // Procesar variable global
                        ProcessGlobalVariable();
                        break;
                    default:
                        throw new Exception($"Opcode desconocido: {opcode} (0x{opcode:X2}) en posición {_ip}");
                }
            }
        }

        private void LoadReturn()
        {
            if (_callStack.Count > 0)
            {
                // Eliminar el contexto de variables locales de esta función
                if (_localVariables.Count > 0)
                {
                    _localVariables.Pop();
                }

                // Obtener la dirección de retorno de la pila de llamadas
                int returnAddress = _callStack.Pop();
                Debug.WriteLine($"Return: Volviendo a la dirección {returnAddress}");

                // Restaurar el puntero de instrucción a la dirección de retorno
                _ip = returnAddress;
            }
            else
            {
                Debug.WriteLine("Return: No hay dirección de retorno (posiblemente en la función principal)");
                _ip++;
            }
        }

        private void LoadBlock()
        {
            Debug.WriteLine($"LoadBlock");
            // Leer el tipo de retorno y avanzar
            _ip++;
            if (_ip < _bytecode.Count)
            {
                byte returnType = _bytecode[_ip];
                Debug.WriteLine($"Enter: Tipo de retorno: {(ConstantType)returnType}");
                _ip++;

                // Leer número de parámetros
                if (_ip < _bytecode.Count)
                {
                    byte paramCount = _bytecode[_ip];
                    Debug.WriteLine($"Enter: Número de parámetros: {paramCount}");
                    _ip++;

                    // Procesar cada parámetro
                    for (int i = 0; i < paramCount; i++)
                    {
                        // Leer tipo de parámetro
                        if (_ip < _bytecode.Count)
                        {
                            byte paramType = _bytecode[_ip];
                            _ip++;

                            // Leer longitud del nombre del parámetro
                            if (_ip < _bytecode.Count)
                            {
                                byte paramNameLength = _bytecode[_ip];
                                _ip++;

                                // Leer nombre del parámetro
                                if (_ip + paramNameLength <= _bytecode.Count)
                                {
                                    string paramName = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, paramNameLength);
                                    Debug.WriteLine($"Enter: Parámetro: {(ConstantType)paramType} {paramName}");
                                    _ip += paramNameLength;
                                }
                                else
                                {
                                    Debug.WriteLine("Error al leer el nombre del parámetro");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Helper method for arithmetic operations
        private void PerformArithmeticOperation(Func<int, int, int> intOperation, Func<float, float, float> floatOperation)
        {
             byte currentOpcode = _bytecode[_ip]; // <-- Añadir esta línea para obtener el opcode actual
    
    if (_stack.Count < 2)
        throw new Exception("No hay suficientes operandos en la pila para la operación aritmética");
    
    // Pop operands (remember stack ordering)
    var b = _stack.Pop();
    var a = _stack.Pop();

            // Perform the operation based on types
            if (a is int intA && b is int intB)
            {
                _stack.Push(intOperation(intA, intB));
                Debug.WriteLine($"Operación aritmética: {intA} op {intB} = {_stack.Peek()}");
            }
            else if (a is float floatA && b is float floatB)
            {
                _stack.Push(floatOperation(floatA, floatB));
                Debug.WriteLine($"Operación aritmética: {floatA} op {floatB} = {_stack.Peek()}");
            }
            else if (a is int ia && b is float fb)
            {
                _stack.Push(floatOperation(ia, fb));
                Debug.WriteLine($"Operación aritmética mixta: {ia} op {fb} = {_stack.Peek()}");
            }
            else if (a is float fa && b is int ib)
            {
                _stack.Push(floatOperation(fa, ib));
                Debug.WriteLine($"Operación aritmética mixta: {fa} op {ib} = {_stack.Peek()}");
            }
            else if (a is string strA && b is string strB && opcode == (byte)OpCode.Add)
            {
                // String concatenation only for addition
                _stack.Push(strA + strB);
                Debug.WriteLine($"Concatenación de cadenas: \"{strA}\" + \"{strB}\" = \"{_stack.Peek()}\"");
            }
            else
            {
                throw new Exception($"Tipos incompatibles para la operación aritmética: {a?.GetType().Name} y {b?.GetType().Name}");
            }

            _ip++; // Move to next instruction
        }

        private void LoadMark()
        {
            _ip++; // Mover a la longitud del nombre
            byte nameLength = _bytecode[_ip];
            _ip++; // Mover al nombre

            if (_ip + nameLength <= _bytecode.Count)
            {
                string functionName = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, nameLength);
                Debug.WriteLine($"Mark: Nombre de función: {functionName}");

                // Avanzar después del nombre
                _ip += nameLength;
            }
            else
            {
                Debug.WriteLine("Error al leer el nombre de la función");
                _ip++;
            }
        }

        private void LoadConstant()
        {
            _ip++; // Move to constant type byte
            if (_ip >= _bytecode.Count)
                throw new Exception("Unexpected end of bytecode while reading constant type");

            ConstantType constantType = (ConstantType)_bytecode[_ip];
            Debug.WriteLine($"LoadConstant: {constantType}");

            switch (constantType)
            {
                case ConstantType.Integer:
                    _ip++; // Move to integer value start
                    if (_ip + 3 >= _bytecode.Count)
                        throw new Exception("Unexpected end of bytecode while reading integer value");

                    int intValue = BitConverter.ToInt32(_bytecode.ToArray(), _ip);
                    _stack.Push(intValue);
                    Debug.WriteLine($"Pushed integer: {intValue}");
                    _ip += 3; // Advance to next instruction (already read 1 byte, need to skip 3 more)
                    break;

                case ConstantType.String:
                    _ip++; // Move to string length byte
                    if (_ip >= _bytecode.Count)
                        throw new Exception("Unexpected end of bytecode while reading string length");

                    ushort stringLength;
                    byte lengthByte = _bytecode[_ip];

                    if (lengthByte > 127) // Using high bit as indicator for 2-byte length
                    {
                        if (_ip + 1 >= _bytecode.Count)
                            throw new Exception("Unexpected end of bytecode while reading string length (2 bytes)");

                        stringLength = BitConverter.ToUInt16(_bytecode.ToArray(), _ip);
                        _ip += 2; // Skip 2 bytes of length
                    }
                    else
                    {
                        stringLength = lengthByte;
                        _ip++; // Skip 1 byte of length
                    }

                    if (_ip + stringLength > _bytecode.Count)
                        throw new Exception("Unexpected end of bytecode while reading string data");

                    string strValue = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, stringLength);
                    _stack.Push(strValue);
                    Debug.WriteLine($"Pushed string: \"{strValue}\"");
                    _ip += stringLength - 1; // Adjust for the main loop increment
                    break;

                case ConstantType.Float:
                    _ip++; // Move to float value start
                    if (_ip + 3 >= _bytecode.Count)
                        throw new Exception("Unexpected end of bytecode while reading float value");

                    float floatValue = BitConverter.ToSingle(_bytecode.ToArray(), _ip);
                    _stack.Push(floatValue);
                    Debug.WriteLine($"Pushed float: {floatValue}");
                    _ip += 3; // Already read 1 byte, need to skip 3 more
                    break;

                case ConstantType.Char:
                    _ip++; // Move to char value
                    if (_ip >= _bytecode.Count)
                        throw new Exception("Unexpected end of bytecode while reading char value");

                    char charValue = (char)_bytecode[_ip];
                    _stack.Push(charValue);
                    Debug.WriteLine($"Pushed char: '{charValue}'");
                    break;

                case ConstantType.Bool:
                    _ip++; // Move to bool value
                    if (_ip >= _bytecode.Count)
                        throw new Exception("Unexpected end of bytecode while reading bool value");

                    bool boolValue = _bytecode[_ip] != 0;
                    _stack.Push(boolValue);
                    Debug.WriteLine($"Pushed boolean: {boolValue}");
                    break;

                case ConstantType.Void:
                    Debug.WriteLine("Void type detected, nothing pushed to stack");
                    break;

                default:
                    throw new Exception($"Unknown constant type: {constantType}");
            }

            _ip++; // Move to next instruction
        }

        private void LoadVariable()
        {
            _ip++; // Mover a la longitud del nombre de la variable

            if (_ip >= _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer la longitud del nombre de la variable");

            byte nameLength = _bytecode[_ip];
            _ip++; // Mover al inicio del nombre de la variable

            if (_ip + nameLength > _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer el nombre de la variable");

            string varName = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, nameLength);
            bool found = false;

            // Primero buscar en las variables locales (si hay un contexto activo)
            if (_localVariables.Count > 0)
            {
                Dictionary<string, object> currentContext = _localVariables.Peek();
                if (currentContext.TryGetValue(varName, out object localValue))
                {
                    _stack.Push(localValue);
                    Debug.WriteLine($"Variable local cargada {varName} = {localValue} ({localValue.GetType().Name})");
                    found = true;
                }
            }

            // Si no se encontró en variables locales, buscar en variables globales
            if (!found && _variables.TryGetValue(varName, out object globalValue))
            {
                _stack.Push(globalValue);
                Debug.WriteLine($"Variable global cargada {varName} = {globalValue} ({globalValue.GetType().Name})");
                found = true;
            }

            if (!found)
            {
                throw new Exception($"Variable no definida: {varName}");
            }

            _ip += nameLength; // Mover más allá del nombre de la variable
        }

        private void StoreVariable()
        {
            if (_stack.Count == 0)
                throw new Exception("No se puede almacenar la variable: la pila está vacía");

            var value = _stack.Pop();
            _ip++; // Mover a la longitud del nombre de la variable

            if (_ip >= _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer la longitud del nombre de la variable");

            byte nameLength = _bytecode[_ip];
            _ip++; // Mover al inicio del nombre de la variable

            if (_ip + nameLength > _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer el nombre de la variable");

            string varName = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, nameLength);
            bool isLocalVariable = false;

            // Verificar si la variable ya existe en el contexto local actual
            if (_localVariables.Count > 0)
            {
                Dictionary<string, object> currentContext = _localVariables.Peek();
                if (currentContext.ContainsKey(varName))
                {
                    isLocalVariable = true;
                }
            }

            // Almacenar el valor en el contexto apropiado
            Dictionary<string, object> targetContext;
            if (isLocalVariable || (_localVariables.Count > 0 && !_variables.ContainsKey(varName)))
            {
                // Si es una variable local existente o estamos en un contexto de función y no es una global ya definida,
                // almacenarla en el contexto local actual
                targetContext = _localVariables.Peek();
                Debug.WriteLine($"Almacenando en variable local: {varName}");
            }
            else
            {
                // Si es una variable global o estamos en el contexto global
                targetContext = _variables;
                Debug.WriteLine($"Almacenando en variable global: {varName}");
            }

            // Store different types of values
            switch (value)
            {
                case int intValue:
                    targetContext[varName] = intValue;
                    Debug.WriteLine($"Variable almacenada {varName} = {intValue} (int)");
                    break;

                case float floatValue:
                    targetContext[varName] = floatValue;
                    Debug.WriteLine($"Variable almacenada {varName} = {floatValue} (float)");
                    break;

                case string stringValue:
                    targetContext[varName] = stringValue;
                    Debug.WriteLine($"Variable almacenada {varName} = {stringValue} (string)");
                    break;

                case char charValue:
                    targetContext[varName] = charValue;
                    Debug.WriteLine($"Variable almacenada {varName} = {charValue} (char)");
                    break;

                case bool boolValue:
                    targetContext[varName] = boolValue;
                    Debug.WriteLine($"Variable almacenada {varName} = {boolValue} (bool)");
                    break;

                default:
                    throw new Exception($"No se puede almacenar un valor de tipo {value.GetType().Name} en la variable {varName}");
            }

            _ip += nameLength; // Mover más allá del nombre de la variable
        }

        private void LoadString()
        {
            if (_stack.Count == 0)
                throw new Exception("No se puede cargar la cadena: la pila está vacía");

            int stringLength = 0;

            if (_stack.Peek() is int length)
            {
                stringLength = length;
                _stack.Pop();
            }
            else
            {
                throw new Exception("La longitud de la cadena debe ser un entero");
            }

            Debug.WriteLine($"Cargando cadena de longitud {stringLength}");

            if (_stack.Count < stringLength)
                throw new Exception($"No hay suficientes bytes en la pila para una cadena de longitud {stringLength}");

            byte[] stringBytes = new byte[stringLength];

            // Sacar los bytes en orden inverso (ya que la pila es LIFO)
            for (int i = stringLength - 1; i >= 0; i--)
            {
                if (_stack.Pop() is byte b)
                {
                    stringBytes[i] = b;
                }
                else
                {
                    throw new Exception("Se esperaba un byte en la pila para cargar la cadena");
                }
            }

            string str = Encoding.UTF8.GetString(stringBytes);
            _stack.Push(str);
            Debug.WriteLine($"Cadena cargada: \"{str}\"");

            _ip++; // Mover a la siguiente instrucción
        }


        // Modificar el método CallFunction para guardar la dirección de retorno
        private void CallFunction()
        {
            _ip++; // Avanzar más allá del opcode CALL

            // Leer la longitud del nombre del método
            if (_ip >= _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer la longitud del nombre del método");

            byte methodNameLength = _bytecode[_ip++];

            // Leer el nombre del método
            if (_ip + methodNameLength > _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer el nombre del método");

            string methodName = Encoding.UTF8.GetString(_bytecode.ToArray(), _ip, methodNameLength);
            _ip += methodNameLength;

            Debug.WriteLine($"Llamando al método: {methodName}");

            // Leer la cantidad de argumentos
            if (_ip >= _bytecode.Count)
                throw new Exception("Fin inesperado del bytecode al leer la cantidad de argumentos");

            byte argCount = _bytecode[_ip++];
            Debug.WriteLine($"El método tiene {argCount} argumentos");

            // Todos los argumentos deberían estar ahora en la pila en orden inverso
            // Así que necesitamos recolectarlos en el orden correcto
            List<object> args = new List<object>();
            for (int i = 0; i < argCount; i++)
            {
                if (_stack.Count > 0)
                {
                    object arg = _stack.Pop();
                    args.Insert(0, arg); // Insertar al principio para invertir el orden
                }
                else
                {
                    throw new Exception($"No hay suficientes valores en la pila para la llamada al método {methodName}");
                }
            }

            // Si es una función definida por el usuario, guardar dirección de retorno y saltar
            if (_functionTable.TryGetValue(methodName, out int functionPosition))
            {
                // Guardar la posición actual como dirección de retorno
                _callStack.Push(_ip);

                // Crear un nuevo contexto de variables locales para esta función
                _localVariables.Push(new Dictionary<string, object>());

                // Saltar a la posición de la función
                _ip = functionPosition;
                Debug.WriteLine($"Saltando a la función definida por el usuario en posición {functionPosition}");
            }
            else
            {
                // Ejecutar método integrado
                CallMethod(methodName, args);
            }
        }

        private void CallMethod(string methodName, List<object> parameters)
        {
            Debug.WriteLine($"Ejecutando el método: {methodName} con {parameters.Count} parámetros");

            switch (methodName)
            {
                case "printf":
                    if (parameters.Count == 1)
                    {
                        Console.WriteLine(parameters[0]?.ToString() ?? "null");
                    }
                    else
                    {
                        throw new Exception($"Printf requiere exactamente 1 parámetro, se recibió {parameters.Count}");
                    }
                    break;

                // Agregar más funciones integradas aquí
                case "add":
                    if (parameters.Count == 2 && parameters[0] is int a && parameters[1] is int b)
                    {
                        _stack.Push(a + b);
                    }
                    else
                    {
                        throw new Exception("Add requiere exactamente 2 parámetros enteros");
                    }
                    break;

                default:
                    throw new Exception($"Método desconocido: {methodName}");
            }
        }
    }
}