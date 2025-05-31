[<AutoOpen>]
module Helpers

open System
open System.Threading
open Starter.SearchEngine
open FluentAvalonia.UI.Controls

type StarterIconSource with
    /// Transform StarterIconSource in FluentAvalonia.UI.Controls.IconSource
    static member build (iconSource: StarterIconSource) =
        match iconSource.Symbol.HasValue, iconSource.SourceImage with
        | true, _ -> SymbolIconSource(Symbol = iconSource.Symbol.Value) :> IconSource | null
        | false, null -> null
        | false, image -> ImageIconSource(Source = image)

    static member buildWithFontSize fontSize (iconSource: StarterIconSource) =
        match iconSource.Symbol.HasValue, iconSource.SourceImage with
        | true, _ -> SymbolIconSource(Symbol = iconSource.Symbol.Value, FontSize = fontSize) :> IconSource | null
        | false, null -> null
        | false, image -> ImageIconSource(Source = image)

let disposeOnCancelled (ct: CancellationToken) (d: IDisposable) =
    fun () -> d.Dispose()
    |> ct.Register
    |> ignore
