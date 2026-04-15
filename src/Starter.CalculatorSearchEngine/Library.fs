namespace Starter.CalculatorSearchEngine

open System
open Avalonia.Input.Platform
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

type CalculatorSearchEngine(clipboard: IClipboard) =
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

                    // TODO: CSharpMath.Avalonia doesn't work with Avalonia 12
                    // match expr with
                    // | Number n when n.IsInteger -> ()
                    // | _ -> { LaTeX = LaTeX.fromExpression expr }
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

    override this.LoadSearchEngineIds() = [| nameof CalculatorSearchEngine |]

    override this.LoadSearchEngine(_, _, _, clipboard) = CalculatorSearchEngine(clipboard), null

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
