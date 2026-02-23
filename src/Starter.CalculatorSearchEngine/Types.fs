namespace Starter.CalculatorSearchEngine.Types

open MathNet.Numerics

[<Struct>]
type Constant = E | I | Pi

type Function =
    | Abs
    | Ln
    | Lg
    | Exp
    | Sin | Cos | Tan
    | Sec | Csc | Cot
    | Sh | Ch | Th
    | Sech | Csch | Coth
    | Asin | Acos | Atan
    | Asec | Acsc | Acot
    | Ash | Ach | Ath
    | Asech | Acsch | Acoth
    | Factorial

type Expression =
    | Constant of Constant
    | Number of BigRational
    | Sum of Expression * Expression
    | Product of Expression * Expression
    | Power of Expression * Expression
    | Function of Function * Expression
    | Undefined

[<RequireQualifiedAccess>]
module Expression =
    let private (|UnFunc|_|) f1 f2 expr =
        match expr with
        | f', Function(f'', e) when f' = f1 && f'' = f2 || f' = f2 && f'' = f1 -> ValueSome e
        | _ -> ValueNone

    let constant = Constant
    let int = BigRational.FromInt >> Number
    let frac x y = BigRational.FromIntFraction(x, y) |> Number

    let rec sum x y =
        match x, y with
        | Number x, Number y -> x + y |> Number
        | Product(x1, x2), Product(y1, y2) when x1 = y1 -> multiply x1 (sum x2 y2)
        | Product(x1, x2), Product(y1, y2) when x1 = y2 -> multiply x1 (sum x2 y1)
        | Product(x1, x2), Product(y1, y2) when x2 = y1 -> multiply x2 (sum x1 y2)
        | Product(x1, x2), Product(y1, y2) when x2 = y2 -> multiply x2 (sum x1 y1)
        | _ -> Sum(x, y)
    and multiply x y =
        match x, y with
        | Number x, Number y -> x * y |> Number
        | Number x, Product(Number n, y)
        | Product(Number n, y), Number x -> Product(Number (x * n), y)
        | Power(base1, exp1), Power(base2, exp2) when base1 = base2 -> Power(base1, sum exp1 exp2)
        | _ -> Product(x, y)

    let negate = function
        | Number n -> Number -n
        | e -> Product(int -1, e)

    let subtract x y = sum x (negate y)

    let pow x y =
        match x, y with
        | Number n, _ when n = BigRational.Zero -> int 0
        | Number n, _ when n = BigRational.One -> int 1

        | _, Number n when n = BigRational.Zero -> int 1
        | x, Number n when n = BigRational.One -> x

        | Number x, Number y when y.IsInteger -> BigRational.Pow(x, BigRational.ToInt32 y) |> Number
        | Power(base', exp), _ -> Power(base', multiply exp y)
        | _ -> Power(x, y)

    let invert x =
        match x with
        | Number x -> x |> BigRational.Reciprocal |> Number
        | _ -> pow x (int -1)

    let divide x y = multiply x (invert y)

    let apply func expr =
        match func, expr with
        | UnFunc Ln Exp e
        | UnFunc Sin Asin e
        | UnFunc Cos Acos e
        | UnFunc Tan Atan e
        | UnFunc Sec Asec e
        | UnFunc Csc Acsc e
        | UnFunc Cot Acot e
        | UnFunc Sh Ash e
        | UnFunc Ch Ach e
        | UnFunc Th Ath e
        | UnFunc Sech Asech e
        | UnFunc Csch Acsch e
        | UnFunc Coth Acoth e
        | UnFunc Asech Sech e
        | UnFunc Acsch Csch e
        | UnFunc Acoth Coth e -> e
        | Lg, Function(Exp, e) -> // lg = ln e / ln 10
            Function(Ln, int 10)
            |> invert
            |> multiply e
        | Abs, Number n -> BigRational.Abs n |> Number
        | _ -> Function(func, expr)
