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
    inherit DynamicSearchEngine()

    let builder = Func<LaTeXSearchResult | null, INameScope, Control | null>(fun dc _ ->
        let latex =
            match dc with
            | null -> null
            | dc -> dc.LaTeX

        ResultControl(LaTeX = latex)
    )

    let controlDataTemplate = FuncDataTemplate<LaTeXSearchResult>(builder, true)

    override this.Id = nameof Calculator
    override this.Name = "Calculator"
    override this.Icon = Icon.icon
    override this.Activators = [| DefaultSearchEngineActivator(this) |]
    override this.BufferResults = false

    override this.Search(query, ct, activator) =
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
                | ValueSome number -> NumberSearchResult number :> SearchResult

                match expr with
                | Number n when n.IsInteger -> ()
                | _ -> LaTeXSearchResult(LaTeX.fromExpression expr, controlDataTemplate)
            }
        }
        |> ValueOption.map (fun s -> struct (s, Observable.Empty()))
        |> ValueOption.defaultValue struct (Seq.empty, Observable.Empty())

    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? LaTeXSearchResult as sr -> ValueSome sr.LaTeX
        | :? NumberSearchResult as sr -> ValueSome sr.Result
        | _ -> ValueNone
        |> ValueOption.iter (clipboard.SetTextAsync >> ignore)

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof Calculator |]
    override this.LoadSearchEngine(_, _, _, clipboard) = Calculator(clipboard), null
