module Starter.CalculatorSearchEngine.Expression

open Starter.CalculatorSearchEngine.Types
open MathNet.Numerics

let private (|ReciprocalFunction|_|) f1 f2 expr =
    match expr with
    | f1', Function(f2', a) when
        f1 = f1' && f2 = f2' || f2 = f1' && f1 = f2' ->
        ValueSome a
    | _ -> ValueNone

let constant = Constant
let int = BigRational.FromInt >> Number
let frac x y = BigRational.FromIntFraction(x, y) |> Number

let rec add x y =
    match x, y with
    | Number x, Number y -> Number (x + y)
    | Product(x1, x2), Product(y1, y2) when x1 = y1 -> multiply x1 (add x2 y2)
    | Product(x1, x2), Product(y1, y2) when x1 = y2 -> multiply x1 (add x2 y1)
    | Product(x1, x2), Product(y1, y2) when x2 = y1 -> multiply x2 (add x1 y2)
    | Product(x1, x2), Product(y1, y2) when x2 = y2 -> multiply x2 (add x1 y1)
    | x, y -> Sum(x, y)

and multiply x y =
    match x, y with
    | Number o, e
    | e, Number o when o.IsOne -> e

    | Number x, Number y -> Number (x * y)
    | Number x, Product(Number n, y) -> multiply (Number (x * n)) y
    | x, Number y -> multiply (Number y) x
    | Power(base1, exp1), Power(base2, exp2) when base1 = base2 ->
        add exp1 exp2
        |> pow base1
    | x, y -> Product(x, y)

and pow b e =
    match b, e with
    | _, Number exp when exp = BigRational.Zero -> int 1
    | b, Number exp when exp = BigRational.One -> b
    | Number b, _ when b = BigRational.Zero -> int 0
    | Number b, _ when b = BigRational.One -> int 1
    | Number b, Number e when e.IsInteger && e.IsNegative ->
        pow
            (b |> BigRational.Reciprocal |> Number)
            (e |> BigRational.Abs |> Number)
    | Power(b, e1), e2 ->
        multiply e1 e2
        |> pow b
    | Constant E, e -> apply Exp e
    | Function(Exp, e1), e2 -> multiply e1 e2 |> apply Exp
    | b, e -> Power(b, e)

and apply f a =
    match f, a with
    | ReciprocalFunction Exp Ln a
    | ReciprocalFunction Cos Acos a
    | ReciprocalFunction Sin Asin a
    | ReciprocalFunction Tan Atan a
    | ReciprocalFunction Sec Asec a
    | ReciprocalFunction Csc Acsc a
    | ReciprocalFunction Cot Acot a
    | ReciprocalFunction Sh Ash a
    | ReciprocalFunction Ch Ach a
    | ReciprocalFunction Th Ath a
    | ReciprocalFunction Sech Asech a
    | ReciprocalFunction Csch Acsch a
    | ReciprocalFunction Coth Acoth a -> a
    | Abs, Number a -> a |> BigRational.Abs |> Number
    | Factorial, arg ->
        match arg with
        | Number n when not n.IsInteger || n.IsNegative -> Undefined
        | expr -> Function(Factorial, expr)
    | f, a -> Function(f, a)

let divide x y = pow y (int -1) |> multiply x
let negate x = multiply (int -1) x
let subtract x y = add x (negate y)
