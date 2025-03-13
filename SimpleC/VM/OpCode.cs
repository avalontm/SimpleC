namespace SimpleC.VM
{
    /// <summary>
    /// Opcodes para la máquina virtual de SimpleC.
    /// El sistema usa un único byte para identificar cada instrucción.
    /// </summary>
    public enum OpCode
    {
        Nop = 0x00,    // No operation

        // Operaciones de carga/almacenamiento (0x10-0x1F)
        LoadC = 0x10,   // Cargar constante en la pila
        Load = 0x11,    // Cargar variable en la pila
        Store = 0x12,   // Almacenar valor de la pila en una variable
        Dup = 0x13,     // Duplicar el valor superior de la pila
        Swap = 0x14,    // Intercambiar los dos valores superiores de la pila
        LoadS = 0x15,   // Cargar string desde bytes en la pila
        LoadGlobal = 0x16,  // Cargar variable global
        StoreGlobal = 0x17, // Almacenar en variable global

        // Operaciones de pila (0x20-0x2F)
        Pop = 0x20,     // Sacar valor superior de la pila

        // Operaciones aritméticas (0x30-0x3F)
        Add = 0x30,     // Suma
        Sub = 0x31,     // Resta
        Mul = 0x32,     // Multiplicación
        Div = 0x33,     // División
        Mod = 0x34,     // Módulo
        Neg = 0x35,     // Negación aritmética
        Inc = 0x36,     // Incremento
        Dec = 0x37,     // Decremento

        // Operaciones de comparación (0x40-0x4F)
        Equal = 0x40,      // Igualdad (==)
        NotEqual = 0x41,   // Diferencia (!=)
        Less = 0x42,       // Menor que (<)
        LessEqual = 0x43,  // Menor o igual (<=)
        Greater = 0x44,    // Mayor que (>)
        GreaterEqual = 0x45, // Mayor o igual (>=)

        // Operaciones lógicas (0x48-0x4F)
        And = 0x48,     // AND lógico
        Or = 0x49,      // OR lógico
        Not = 0x4A,     // NOT lógico

        // Operaciones de salto (0x50-0x5F)
        Jump = 0x50,        // Salto incondicional
        JumpIfFalse = 0x51, // Saltar si falso
        JumpIfTrue = 0x52,  // Saltar si verdadero

        // Operaciones de función (0x60-0x6F)
        Call = 0x60,    // Llamada a función

        // Operaciones de definición de funciones (0x80-0x8F)
        Mark = 0x80,    // Marcar inicio de función
        Enter = 0x81,   // Entrada a bloque de función
        Exit = 0x82,    // Salida de bloque de función
        Return = 0x85,  // Retorno de función

        // Operaciones de control de flujo específicas (0x90-0x9F)
        Break = 0x90,   // Romper bucle
        Continue = 0x91,// Continuar con la siguiente iteración

        // Control de ejecución (0xF0-0xFF)
        Halt = 0xFF,    // Detener la ejecución
    }

}