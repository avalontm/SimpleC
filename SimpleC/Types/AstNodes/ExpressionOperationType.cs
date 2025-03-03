namespace SimpleC.Types.AstNodes
{
    // Necesitamos el enum adicionalmente a OperatorType para admitir el analizador,
    // que debe decidir entre el menos unario y binario, y admitir llamadas a funciones,
    // las cuales no son "operadores" en el sentido de los tokens.
    enum ExpressionOperationType
    {
        Add,            // Suma
        Substract,      // Resta
        Multiply,       // Multiplicación
        Divide,         // División
        Modulo,         // Módulo
        Assignment,     // Asignación (=)
        Equals,         // Igualdad (==)
        GreaterThan,    // Mayor que (>)
        LessThan,       // Menor que (<)
        GreaterEquals,  // Mayor o igual (>=)
        LessEquals,     // Menor o igual (<=)
        NotEquals,      // Distinto (!=)
        Not,            // Negación lógica (!)
        And,            // AND lógico (&&)
        Or,             // OR lógico (||)
        Negate,         // Negación numérica (-)
        FunctionCall,   // Llamada a función
        OpenBrace,      // Paréntesis de apertura (
    }
}
