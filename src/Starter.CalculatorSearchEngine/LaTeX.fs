module Starter.CalculatorSearchEngine.LaTeX

open Starter.CalculatorSearchEngine.Types
open System.Text
open MathNet.Numerics

let inline private betweenParenthesis (str: StringBuilder) content =
    str.Append "\\left(" |> ignore
    content()
    str.Append "\\right)" |> ignore

type StringBuilder with
    member this.AppendFunction (f: string) content =
        this.Append(f) |> ignore
        betweenParenthesis this content

let rec isNextNumber = function
    | Number _ -> true
    | Undefined
    | Function _
    | Constant _ -> false
    | Sum(e, _)
    | Product(e, _)
    | Power(e, _) -> isNextNumber e

let rec fromExpression expr =
    let str = StringBuilder()

    let rec loop precedence (str: StringBuilder) = function
        | Undefined -> str.Append "?" |> ignore
        | Constant E -> str.Append "e" |> ignore
        | Constant I -> str.Append "i" |> ignore
        | Constant Pi -> str.Append "\\pi" |> ignore
        | Number n when n.IsInteger -> str.Append n.Numerator |> ignore
        | Number n ->
            str.Append "\\frac{" |> ignore
            str.Append n.Numerator |> ignore
            str.Append "}{" |> ignore
            str.Append n.Denominator |> ignore
            str.Append "}" |> ignore
        | Function(Ln, e) -> str.AppendFunction "\\ln" (fun () -> loop 0 str e)
        | Function(Lg, e) -> str.AppendFunction "\\log" (fun () -> loop 0 str e)
        | Function(Sin, e) -> str.AppendFunction "\\sin" (fun () -> loop 0 str e)
        | Function(Cos, e) -> str.AppendFunction "\\cos" (fun () -> loop 0 str e)
        | Function(Tan, e) -> str.AppendFunction "\\tan" (fun () -> loop 0 str e)
        | Function(Sec, e) -> str.AppendFunction "\\sec" (fun () -> loop 0 str e)
        | Function(Csc, e) -> str.AppendFunction "\\csc" (fun () -> loop 0 str e)
        | Function(Cot, e) -> str.AppendFunction "\\cot" (fun () -> loop 0 str e)
        | Function(Sh, e) -> str.AppendFunction "\\sinh" (fun () -> loop 0 str e)
        | Function(Ch, e) -> str.AppendFunction "\\cosh" (fun () -> loop 0 str e)
        | Function(Th, e) -> str.AppendFunction "\\tanh" (fun () -> loop 0 str e)
        | Function(Sech, e) -> str.AppendFunction "\\operatorname{sech}" (fun () -> loop 0 str e)
        | Function(Csch, e) -> str.AppendFunction "\\operatorname{csch}" (fun () -> loop 0 str e)
        | Function(Coth, e) -> str.AppendFunction "\\coth" (fun () -> loop 0 str e)
        | Function(Asin, e) -> str.AppendFunction "\\arcsin" (fun () -> loop 0 str e)
        | Function(Acos, e) -> str.AppendFunction "\\arccos" (fun () -> loop 0 str e)
        | Function(Atan, e) -> str.AppendFunction "\\arctan" (fun () -> loop 0 str e)
        | Function(Asec, e) -> str.AppendFunction "\\arcsec" (fun () -> loop 0 str e)
        | Function(Acsc, e) -> str.AppendFunction "\\arccsc" (fun () -> loop 0 str e)
        | Function(Acot, e) -> str.AppendFunction "\\arccot" (fun () -> loop 0 str e)
        | Function(Ash, e) -> str.AppendFunction "\\operatorname{arsinh}" (fun () -> loop 0 str e)
        | Function(Ach, e) -> str.AppendFunction "\\operatorname{arcosh}" (fun () -> loop 0 str e)
        | Function(Ath, e) -> str.AppendFunction "\\operatorname{artanh}" (fun () -> loop 0 str e)
        | Function(Asech, e) -> str.AppendFunction "\\operatorname{arsech}" (fun () -> loop 0 str e)
        | Function(Acsch, e) -> str.AppendFunction "\\operatorname{arcsch}" (fun () -> loop 0 str e)
        | Function(Acoth, e) -> str.AppendFunction "\\operatorname{arcoth}" (fun () -> loop 0 str e)
        | Function(Exp, e) -> loop precedence str (Power(Constant E, e))
        | Function(Abs, e) ->
            str.Append "\\left\\vert{" |> ignore
            loop 0 str e
            str.Append "}\\right\\vert" |> ignore

        | Function(Factorial, e) ->
            match e with
            | Sum _
            | Product _
            | Power _ -> betweenParenthesis str (fun () -> loop 4 str e)
            | _ -> loop 4 str e
            str.Append "!" |> ignore

        | Power(base', Number exp) when exp = BigRational.FromIntFraction(1, 2) ->
            str.Append "\\sqrt{" |> ignore
            loop 3 str base'
            str.Append "}" |> ignore

        | Power(base', exp) ->
            if 3 < precedence then
                str.Append "\\left(" |> ignore
            str.Append "{" |> ignore
            loop 3 str base'
            str.Append "}^{" |> ignore
            loop 0 str exp
            str.Append "}" |> ignore
            if 3 < precedence then
                str.Append "\\right)" |> ignore

        | Product(e1, Power(e2, Number n)) when n = BigRational.FromInt -1 ->
            str.Append "\\frac{" |> ignore
            loop 0 str e1
            str.Append "}{" |> ignore
            loop 0 str e2
            str.Append "}" |> ignore

        | Product(Number n, e)
        | Product(e, Number n) ->
            if 2 < precedence then
                str.Append "\\left(" |> ignore
            str.Append "{" |> ignore
            loop 2 str (Number n)

            match e |> isNextNumber with
            | true -> str.Append "}\\times{" |> ignore
            | false -> str.Append "}{" |> ignore

            loop 2 str e
            str.Append "}" |> ignore
            if 2 < precedence then
                str.Append "\\right)" |> ignore

        | Product(e1, e2) ->
            if 2 < precedence then
                str.Append "\\left(" |> ignore
            str.Append "{" |> ignore
            loop 2 str e1
            str.Append "}{" |> ignore
            loop 2 str e2
            str.Append "}" |> ignore
            if 2 < precedence then
                str.Append "\\right)" |> ignore

        | Sum(e1, e2) ->
            if 1 < precedence then
                str.Append "\\left(" |> ignore
            str.Append "{" |> ignore
            loop 1 str e1
            str.Append "}+{" |> ignore
            loop 1 str e2
            str.Append "}" |> ignore
            if 1 < precedence then
                str.Append "\\right)" |> ignore

    loop 0 str expr
    str.ToString()
