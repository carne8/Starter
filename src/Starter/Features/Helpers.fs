[<AutoOpen>]
module Helpers

open System
open System.Threading
open Starter.SearchEngine

open Avalonia.Controls
open FluentAvalonia.UI.Controls
open FluentIcons.Common
open FluentIcons.Avalonia.Fluent

type StarterIconSource with
    /// Transform StarterIconSource in Control
    static member build (iconSource: StarterIconSource) : Control | null =
        match iconSource.Icon.HasValue, iconSource.Image with
        | true, _ -> FluentIcon(Icon = iconSource.Icon.Value)
        | false, null -> null
        | false, image -> ImageIcon(Source = image)

    static member buildWithFontSize iconSize (iconSource: StarterIconSource) : Control | null =
        match iconSource.Icon.HasValue, iconSource.Image with
        | true, _ -> FluentIcon(Icon = iconSource.Icon.Value, FontSize = iconSize, IconSize = IconSize.Resizable)
        | false, null -> null
        | false, image -> ImageIcon(Source = image)

    /// Transform StarterIconSource in IconSource
    static member buildIconSource (iconSource: StarterIconSource) : IconSource | null =
        match iconSource.Icon.HasValue, iconSource.Image with
        | true, _ -> FluentIconSource(Icon = iconSource.Icon.Value)
        | false, null -> null
        | false, image -> ImageIconSource(Source = image)

let disposeOnCancelled (ct: CancellationToken) (d: IDisposable) =
    fun () -> d.Dispose()
    |> ct.Register
    |> ignore

[<RequireQualifiedAccess>]
module Observable =
    open R3

    let inline map ([<InlineIfLambda>] f: 'A -> 'B) (obs: Observable<'A>) : Observable<'B>  = obs.Select(f)
    let inline subscribe ([<InlineIfLambda>] f: 'A -> unit) (obs: Observable<'A>)  = obs.Subscribe(f)
