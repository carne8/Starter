module Starter.CalculatorSearchEngine.Simplifications

open Starter.CalculatorSearchEngine.Types
open System.Numerics
open MathNet.Numerics

let private (|ReciprocalFunction|_|) f1 f2 expr =
    match expr with
    | Function(f1', Function(f2', a)) when
        f1 = f1' && f2 = f2' || f2 = f1' && f1 = f2' ->
        ValueSome a
    | _ -> ValueNone

module Trigo =
    let zero = Expression.int 0
    let one = Expression.int 1
    let sqrt3div2 =
        Expression.divide
            (Power(Expression.int 3, Expression.frac 1 2))
            (Expression.int 2)
    let sqrt2div2 =
        Expression.divide
            (Power(Expression.int 2, Expression.frac 1 2))
            (Expression.int 2)
    let half = Expression.frac 1 2

    let normalizeAngle (angle: BigRational) =
        if angle.IsInteger then
            if angle.Numerator.IsEven then 0 else 1
            |> BigRational.FromInt
        else
            BigRational.FromBigIntFraction(
                BigInteger.Remainder(angle.Numerator, angle.Denominator),
                angle.Denominator
            )

    let simplifyCos = function
        | Number n when n.IsZero -> one
        | Constant Pi -> Expression.negate one
        | Product(Constant Pi, Number r)
        | Product(Number r, Constant Pi) ->
            let r = normalizeAngle r

            if r = BigRational.Zero then one
            elif r = BigRational.FromIntFraction(1, 6) then sqrt3div2
            elif r = BigRational.FromIntFraction(1, 4) then sqrt2div2
            elif r = BigRational.FromIntFraction(1, 3) then half
            elif r = BigRational.FromIntFraction(1, 2) then zero
            elif r = BigRational.FromIntFraction(2, 3) then half |> Expression.negate
            elif r = BigRational.FromIntFraction(3, 4) then sqrt2div2 |> Expression.negate
            elif r = BigRational.FromIntFraction(5, 6) then sqrt3div2 |> Expression.negate
            elif r = BigRational.One then one |> Expression.negate
            elif r = -BigRational.FromIntFraction(1, 6) then sqrt3div2
            elif r = -BigRational.FromIntFraction(1, 4) then sqrt2div2
            elif r = -BigRational.FromIntFraction(1, 3) then half
            elif r = -BigRational.FromIntFraction(1, 2) then zero
            elif r = -BigRational.FromIntFraction(2, 3) then half |> Expression.negate
            elif r = -BigRational.FromIntFraction(3, 4) then sqrt2div2 |> Expression.negate
            elif r = -BigRational.FromIntFraction(5, 6) then sqrt3div2 |> Expression.negate
            elif r = -BigRational.One then one |> Expression.negate
            else Function(Cos, Product(Number r, Constant Pi))
        | arg -> Function(Cos, arg)

    let simplifySin arg =
        match arg with
        | Number n when n.IsZero -> zero
        | Constant Pi -> zero
        | Product(Constant Pi, Number r)
        | Product(Number r, Constant Pi) ->
            BigRational.FromIntFraction(1, 2) - r
            |> Number
            |> Expression.multiply (Constant Pi)
            |> simplifyCos
        | arg -> Function(Sin, arg)

    let simplifyTan arg =
        match arg with
        | Number n when n.IsZero -> zero
        | Constant Pi -> zero
        | Product(Constant Pi, Number _)
        | Product(Number _, Constant Pi) ->
            match simplifyCos arg with
            | Number n when n.IsZero -> Undefined
            | cos -> Expression.divide (simplifySin arg) cos
        | arg -> Function(Tan, arg)

let rec simplify expr =
    match expr with
    | Constant _
    | Number _
    | Undefined -> expr
    | Sum(e1, e2) -> Expression.add (simplify e1) (simplify e2)
    | Product(e1, e2) -> Expression.multiply (simplify e1) (simplify e2)
    | Power(e1, e2) -> Expression.pow (simplify e1) (simplify e2)
    | Function(Cos, arg) -> arg |> simplify |> Trigo.simplifyCos
    | Function(Sin, arg) -> arg |> simplify |> Trigo.simplifySin
    | Function(Tan, arg) ->
        match arg with
        | Function(Asin, a) -> Expression.divide a (Expression.apply Cos arg) |> simplify
        | Function(Acos, a) -> Expression.divide (Expression.apply Sin arg) a |> simplify
        | arg -> arg |> simplify |> Trigo.simplifyTan

    | Function(f, e) ->
        e
        |> simplify
        |> Expression.apply f
