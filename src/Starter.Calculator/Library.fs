namespace Starter.Calculator

open System
open Avalonia.Input.Platform
open MathNet.Numerics
open Avalonia.Controls
open Avalonia.Controls.Templates
open FsToolkit.ErrorHandling
open R3

open Starter.SearchEngine
open Starter.Calculator
open Starter.Calculator.Types
open Starter.Calculator.Controls
open Starter.Calculator.Simplifications

type Calculator(clipboard: IClipboard) =
    let builder = Func<LaTeXSearchResult | null, INameScope, Control | null>(fun dc _ ->
        let latex =
            match dc with
            | null -> null
            | dc -> dc.LaTeX

        ResultControl(LaTeX = latex)
    )

    let controlDataTemplate = FuncDataTemplate<LaTeXSearchResult>(builder, true)

    interface IDynamicSearchEngine with
        member this.Id = nameof Calculator
        member this.Name = "Calculator"
        member this.ShortName = "Calculator"
        member this.Icon = Icon.icon
        member this.Activators = [| DefaultSearchEngineActivator(this) |]
        member this.ResultsPriority = ResultPriority.Unique
        member this.BufferResults = false

        member this.Search(query, ct, activator) =
            voption {
                let! expr =
                    query
                    |> Parser.tryParse
                    |> Result.map simplify
                    |> Result.toValueOption

                let result = expr |> Evaluate.evaluate
                let number =
                    if result.IsReal() then
                        result.Real |> string |> ValueSome
                    elif result.IsNaN() then
                        ValueNone
                    else
                        ValueSome $"Re: {result.Real}; Im: {result.Imaginary}"

                return seq {
                    match number with
                    | ValueNone -> ()
                    | ValueSome number -> { Result = number } :> ISearchResult

                    match expr with
                    | Number n when n.IsInteger -> ()
                    | _ -> { LaTeX = LaTeX.fromExpression expr
                             DataTemplate = controlDataTemplate }
                }
            }
            |> ValueOption.map (fun s -> struct (s, Observable.Empty()))
            |> ValueOption.defaultValue struct (Seq.empty, Observable.Empty())

        member this.SearchResultSelected(selectedSearchResult) =
            match selectedSearchResult with
            | :? LaTeXSearchResult as sr -> ValueSome sr.LaTeX
            | :? NumberSearchResult as sr -> ValueSome sr.Result
            | _ -> ValueNone
            |> ValueOption.iter (clipboard.SetTextAsync >> ignore)

        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof Calculator |]
    override this.LoadSearchEngine(_, _, _, clipboard) = Calculator(clipboard), null
