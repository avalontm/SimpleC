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
        LoadS = 0x15,   // Cargar string desde bytes en la pila

        // Operaciones de pila (0x20-0x2F)
        Pop = 0x20,     // Sacar valor superior de la pila

        // Operaciones aritméticas (0x30-0x3F)
        Add = 0x30,     // Suma
        Sub = 0x31,     // Resta
        Mul = 0x32,     // Multiplicación
        Div = 0x33,     // División
        Mod = 0x34,     // Módulo

        // Operaciones de comparación (0x40-0x4F)
        Eq = 0x40,      // Igualdad (==)
        Ne = 0x41,      // Diferencia (!=)
        Lt = 0x42,      // Menor que (<)
        Le = 0x43,      // Menor o igual (<=)
        Gt = 0x44,      // Mayor que (>)
        Ge = 0x45,      // Mayor o igual (>=)

        // Operaciones de salto (0x50-0x5F)
        Jmp = 0x50,     // Salto incondicional
        JmpF = 0x51,    // Saltar si falso
        JmpT = 0x52,    // Saltar si verdadero

        // Operaciones de función (0x60-0x6F)
        Call = 0x60,    // Llamada a función

        StoreGlobal = 0x70,
        LoadGlobal = 0x71,

        // Operaciones de definición de funciones (0x80-0x8F)
        Mark = 0x80,    // Marcar inicio de función
        Enter = 0x81,   // Entrada a bloque de función
        Return = 0x85,  // Retorno de función

        // Control de ejecución (0xF0-0xFF)
        Halt = 0xFF,     // Detener la ejecución,
    }
}