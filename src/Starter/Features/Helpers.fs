[<AutoOpen>]
module Helpers

open Starter.SearchEngine
open FluentAvalonia.UI.Controls

type StarterIconSource with
    /// Transform StarterIconSource in FluentAvalonia.UI.Controls.IconSource
    static member build (iconSource: StarterIconSource) =
        match iconSource.Symbol.HasValue with
        | true -> SymbolIconSource(Symbol = iconSource.Symbol.Value) :> IconSource
        | false -> ImageIconSource(Source = iconSource.SourceImage)

    static member buildWithFontSize fontSize (iconSource: StarterIconSource) =
        match iconSource.Symbol.HasValue with
        | true -> SymbolIconSource(Symbol = iconSource.Symbol.Value, FontSize = fontSize) :> IconSource
        | false -> ImageIconSource(Source = iconSource.SourceImage)
