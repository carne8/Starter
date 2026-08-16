module Starter.Calculator.Parser

open FParsec
open System.Numerics
open MathNet.Numerics
open Starter.Calculator.Types

let private pinteger = pint64 |>> (BigRational.FromBigInt >> Number)
let private pdecimal =
    // intPart + decimalPart/10^decimalPart.Length
    pinteger
    .>>.? (skipAnyOf ".," >>. many1Chars digit)
    |>> fun (intPart, decimalPart) ->
        Sum(
            intPart,
            Number <| BigRational.FromBigIntFraction(
                int64 decimalPart,
                BigInteger.Pow(10, decimalPart.Length)
            )
        )

let private pnumber = pdecimal <|> pinteger

let private pconstant =
    pstringCI "e" >>% Constant E
    <|> (pstringCI "i" >>% Constant I)
    <|> (pstringCI "pi" >>% Constant Pi)

let private pfunction pexpr =
    [ skipStringCI "sqrt(" >>% Sqrt
      skipStringCI "abs(" >>% Abs
      skipStringCI "ln(" >>% Ln
      skipStringCI "lg(" >>% Lg
      skipStringCI "log(" >>% Lg
      skipStringCI "exp(" >>% Exp
      skipStringCI "sin(" >>% Sin
      skipStringCI "cos(" >>% Cos
      skipStringCI "tan(" >>% Tan
      skipStringCI "csc(" >>% Csc
      skipStringCI "sec(" >>% Sec
      skipStringCI "cot(" >>% Cot

      skipStringCI "sh(" >>% Sh
      skipStringCI "sinh(" >>% Sh
      skipStringCI "ch(" >>% Ch
      skipStringCI "cosh(" >>% Ch
      skipStringCI "th(" >>% Th
      skipStringCI "tanh(" >>% Th

      skipStringCI "csch(" >>% Csch
      skipStringCI "sech(" >>% Sech
      skipStringCI "coth(" >>% Coth

      skipStringCI "asin(" >>% Asin
      skipStringCI "arcsin(" >>% Asin
      skipStringCI "acos(" >>% Acos
      skipStringCI "arccos(" >>% Acos
      skipStringCI "atan(" >>% Atan
      skipStringCI "arctan(" >>% Atan
      skipStringCI "acsc(" >>% Acsc
      skipStringCI "arccsc(" >>% Acsc
      skipStringCI "asec(" >>% Asec
      skipStringCI "arcsec(" >>% Asec
      skipStringCI "acot(" >>% Acot
      skipStringCI "arccot(" >>% Acot

      skipStringCI "ash(" >>% Ash
      skipStringCI "asinh(" >>% Ash
      skipStringCI "arsinh(" >>% Ash
      skipStringCI "ach(" >>% Ach
      skipStringCI "acosh(" >>% Ach
      skipStringCI "arcosh(" >>% Ach
      skipStringCI "ath(" >>% Ath
      skipStringCI "atanh(" >>% Ath
      skipStringCI "artanh(" >>% Ath
      skipStringCI "acsch(" >>% Acsch
      skipStringCI "asech(" >>% Asech
      skipStringCI "acoth(" >>% Acoth
      // skipStringCI "airyai(" >>% AiryAi // Airy function Ai
      // skipStringCI "airyaiprime(" >>% AiryAiPrime // Derivative of Airy function Ai
      // skipStringCI "airybi(" >>% AiryBi // Airy function Bi
      // skipStringCI "airybiprime(" >>% AiryBiPrime (* Derivative of Airy function Bi *)
    ]
    |> choice
    .>>. pexpr .>> skipChar ')'
    |>> fun (func, expr) -> Expression.apply func expr

let private opp = OperatorPrecedenceParser<Expression, unit, unit>()
opp.TermParser <-
    choice [
        pfunction opp.ExpressionParser
        pconstant
        pnumber
        between (skipChar '(' .>> spaces) (skipChar ')' .>> spaces) opp.ExpressionParser
        between (skipChar '[' .>> spaces) (skipChar ']' .>> spaces) opp.ExpressionParser
        between (skipChar '|' .>> spaces) (skipChar '|' .>> spaces) opp.ExpressionParser |>> Expression.apply Abs
    ]
    .>> spaces

opp.AddOperator(InfixOperator("+", spaces, 1, Associativity.Left, Expression.add))
opp.AddOperator(InfixOperator("-", spaces, 1, Associativity.Left, Expression.subtract))
opp.AddOperator(InfixOperator("*", spaces, 2, Associativity.Left, Expression.multiply))
opp.AddOperator(InfixOperator("/", spaces, 2, Associativity.Left, Expression.divide))
opp.AddOperator(InfixOperator("^", spaces, 3, Associativity.Left, Expression.pow))
opp.AddOperator(PrefixOperator("-", spaces, 4, false, Expression.negate))
opp.AddOperator(PostfixOperator("!", spaces, 4, true, Expression.apply Factorial))

let tryParse str =
    try
        str
        |> run (opp.ExpressionParser .>> eof)
        |> function
            | Success(r, _, _) -> Result.Ok r
            | Failure(e, _, _) -> Result.Error e
    with e -> Result.Error e.Message
