﻿namespace SimpleC.Types.Tokens
{
    enum OperatorType
    {
        Add,
        SubstractNegate, // negate would have the same string representation as substract
        Multiply,
        Divide,
        Modulo,
        Assignment,
        Equals,
        GreaterThan,  // >
        LessThan,     // <
        GreaterEquals,
        LessEquals,
        NotEquals,
        Not,
        And,
        Or,
        Preprocessor,
        Quotes
    }
}
