namespace Starter.CalculatorSearchEngine

open System
open MathNet.Numerics
open Avalonia.Controls
open Avalonia.Controls.Templates
open FsToolkit.ErrorHandling
open R3

open Starter.SearchEngine
open Starter.CalculatorSearchEngine
open Starter.CalculatorSearchEngine.Types
open Starter.CalculatorSearchEngine.Controls
open Starter.CalculatorSearchEngine.Simplifications

type CalculatorSearchEngine() =
    interface IDynamicSearchEngine with
        member this.Id = nameof CalculatorSearchEngine
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
                    |> Result.toValueOption
                let expr = expr |> simplify

                let result = expr |> Evaluate.evaluate
                let number =
                    match result.IsReal() with
                    | true -> result.Real |> string
                    | false -> $"Re: {result.Real}; Im: {result.Imaginary}"

                return seq { { LaTeX = LaTeX.fromExpression expr } :> ISearchResult
                             { Result = number } }
            }
            |> ValueOption.map (fun s -> struct (s, Observable.Empty()))
            |> ValueOption.defaultValue struct (Seq.empty, Observable.Empty())

        member this.SearchResultSelected(selectedSearchResult) =
            ()
            // match selectedSearchResult with
            // | :? SearchResult as sr ->
            // | _ -> ()

        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof CalculatorSearchEngine |]

    override this.LoadSearchEngine(_, _, _) = CalculatorSearchEngine(), null

    override this.LoadDataTemplates() =
        let builder = Func<LaTeXSearchResult | null, INameScope, Control | null>(fun dc _ ->
            let latex =
                match dc with
                | null -> null
                | dc -> dc.LaTeX

            ResultControl(LaTeX = latex)
        )

        FuncDataTemplate<LaTeXSearchResult>(builder, true)
        :> IDataTemplate
        |> Seq.singleton
