namespace SimpleC.VM
{
    public enum OpCode : byte
    {
        // Basic operations
        Nop = 0x00,    // No operation
       

        // Stack operations
        LoadC = 0x10,  // Load constant onto stack
        Load = 0x11,   // Load variable onto stack
        Store = 0x12,  // Store value from stack to variable
        Dup = 0x13,    // Duplicate top value on stack
        LoadS = 0x15,  // Load string
       

        // Stack manipulation
        Pop = 0x20,    // Pop value from stack
        Global = 0x21, // Load Global

        // Arithmetic operations
        Add = 0x30,    // Addition
        Sub = 0x31,    // Subtraction
        Mul = 0x32,    // Multiplication
        Div = 0x33,    // Division

        // Function operations
        Call = 0x60,   // Call function

        // Control operations
        Mark = 0x80,   // Mark function definition
        Enter = 0x81,  // Enter function
        Return = 0x85, // Return from function

        // Program control
        Halt = 0xFF,    // Halt program execution
    }
}
