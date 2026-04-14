module Starter.CalculatorSearchEngine.Evaluate

open System.Numerics
open Starter.CalculatorSearchEngine.Types

open System
open MathNet.Numerics

let rec evaluate expr =
    match expr with
    | Undefined -> complex nan nan
    | Constant E -> complex Math.E 0
    | Constant Pi -> complex Math.PI 0
    | Constant I -> complex 0 1
    | Number n -> n |> BigRational.ToDouble |> fun n -> complex n 0
    | Sum(e1, e2) -> evaluate e1 + evaluate e2
    | Product(e1, e2) -> evaluate e1 * evaluate e2
    | Power(e1, e2) -> Complex.pow (evaluate e2) (evaluate e1)
    | Function(Ln, e) -> e |> evaluate |> Complex.ln
    | Function(Lg, e) -> e |> evaluate |> Complex.log10
    | Function(Sin, e) -> e |> evaluate |> Complex.sin
    | Function(Cos, e) -> e |> evaluate |> Complex.cos
    | Function(Tan, e) -> e |> evaluate |> Complex.tan
    | Function(Sec, e) -> e |> evaluate |> Complex.sec
    | Function(Csc, e) -> e |> evaluate |> Complex.csc
    | Function(Cot, e) -> e |> evaluate |> Complex.cot
    | Function(Sh, e) -> e |> evaluate |> Complex.sinh
    | Function(Ch, e) -> e |> evaluate |> Complex.cosh
    | Function(Th, e) -> e |> evaluate |> Complex.tanh
    | Function(Sech, e) -> e |> evaluate |> Complex.sech
    | Function(Csch, e) -> e |> evaluate |> Complex.csch
    | Function(Coth, e) -> e |> evaluate |> Complex.coth
    | Function(Asin, e) -> e |> evaluate |> Complex.asin
    | Function(Acos, e) -> e |> evaluate |> Complex.acos
    | Function(Atan, e) -> e |> evaluate |> Complex.atan
    | Function(Asec, e) -> e |> evaluate |> Complex.asec
    | Function(Acsc, e) -> e |> evaluate |> Complex.acsc
    | Function(Acot, e) -> e |> evaluate |> Complex.acot
    | Function(Ash, e) -> e |> evaluate |> Complex.asinh
    | Function(Ach, e) -> e |> evaluate |> Complex.acosh
    | Function(Ath, e) -> e |> evaluate |> Complex.atanh
    | Function(Asech, e) -> e |> evaluate |> Complex.asech
    | Function(Acsch, e) -> e |> evaluate |> Complex.acsch
    | Function(Acoth, e) -> e |> evaluate |> Complex.acoth
    | Function(Abs, e) -> e |> evaluate |> Complex.magnitude |> fun e -> complex e 0
    | Function(Exp, e) -> e |> evaluate |> Complex.exp
    | Function(Factorial, Number n) when n.IsInteger && n.IsPositive ->
        let n = BigRational.ToBigInt n
        if n > bigint 170 then
            Complex.NaN
        else
            n
            |> SpecialFunctions.Factorial
            |> fun n -> complex (BigInteger.op_Explicit n) 0

    | Function(Factorial, e) ->
        let arg = evaluate e

        if arg.IsRealNonNegative() && Complex.IsInteger arg && arg.Real < 170 then
            arg.Real
            |> int
            |> SpecialFunctions.Factorial
            |> fun n -> complex n 0
        else
            Complex.NaN
