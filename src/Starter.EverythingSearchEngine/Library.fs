namespace Starter.EverythingSearchEngine

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open System.Text
open System.Threading
open System.Threading.Tasks

open EverythingAPI
open IconHelper
open Helpers
open Starter.SearchEngine

open Avalonia.Svg.Skia
open R3
open Vanara
open Vanara.Windows.Shell

[<Struct>]
type SearchResult =
    { Name: string
      Path: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = $"everything:{this.Path}"
        member this.Name = this.Name
        member this.Description = this.Path
        member this.Keywords = null
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty

type EverythingSearchEngine(icon, api: IEverything) =
    let pathStrBuilder = StringBuilder(300)
    let resultsObservable = new Subject<_>()

    let loadResultIcon (path: string) =
        try
            use shellItem = new ShellItem(path)
            use hBitmap =
                try shellItem.Images.GetImage(PInvoke.SIZE(128, 128), ShellItemGetImageOptions.ThumbnailOnly)
                with _ ->
                    shellItem.Images.GetImage(PInvoke.SIZE(128, 128))

            let bitmap = hBitmap.ToAvaloniaBitmap()
            StarterIconSource(bitmap, bitmap)
        with _ -> icon

    let readResult i =
        let name =
            i
            |> api.GetResultFileName
            |> Marshal.PtrToStringUni

        match name with
        | null -> ValueNone
        | name ->
            pathStrBuilder.Clear() |> ignore
            api.GetResultFullPathName i pathStrBuilder (uint pathStrBuilder.Capacity)
            let path = pathStrBuilder.ToString()

            let icon = loadResultIcon path

            { Name = name
              Path = path
              Icon = icon }
            :> ISearchResult
            |> ValueSome

    let queryEverything (ct: CancellationToken) maxResultsCount query =
        Task.Run<unit>(fun () -> earlyReturn {
            // Query Everything
            do! api.SetSearch query = CallResult.OK
            api.SetRequestFlags (RequestFlags.FILE_NAME ||| RequestFlags.PATH)
            api.SetMax maxResultsCount
            do! api.Query true

            // Gather results
            let count = api.GetNumResults()

            let mutable i = 0u
            while i < count && not ct.IsCancellationRequested do
                i
                |> readResult
                |> ValueOption.filter (fun _ -> not ct.IsCancellationRequested) // Recheck because `readResult` takes time
                |> ValueOption.iter (Seq.singleton >> resultsObservable.OnNext)
                i <- i + 1u
        }) |> ignore

    interface IDynamicSearchEngine with
        member this.Id = nameof EverythingSearchEngine
        member this.Name = "Everything"
        member this.ShortName = "Everything"
        member this.Icon = icon
        member this.Activators = [| DefaultSearchEngineActivator(this) |]
        member this.ResultsPriority = ResultPriority.Search
        member this.BufferResults = true

        member this.Search(query, ct, activator) =
            if activator <> null then
                queryEverything ct 300u query
            elif query |> String.IsNullOrWhiteSpace |> not then
                queryEverything ct 30u query

            Seq.empty, resultsObservable

        member this.SearchResultSelected(selectedSearchResult) =
            match selectedSearchResult with
            | :? SearchResult as sr ->
                ProcessStartInfo(sr.Path, UseShellExecute = true)
                |> Process.Start
                |> function null -> () | d -> d.Dispose()
            | _ -> ()

        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    let api =
        match RuntimeInformation.ProcessArchitecture with
        | Architecture.X86 -> Everything32() :> IEverything |> ValueSome
        | Architecture.X64 -> Everything64() :> IEverything |> ValueSome
        | Architecture.Arm64 -> EverythingArm64() :> IEverything |> ValueSome
        | _ -> ValueNone

    override this.LoadSearchEngineIds() = [| nameof EverythingSearchEngine |]

    override this.LoadSearchEngine(_, _, _) =
        if OperatingSystem.IsWindows() |> not then
            raise <| PlatformNotSupportedException("Unsupported OS")

        match api with
        | ValueNone -> raise <| PlatformNotSupportedException()
        | ValueSome api ->
            let svgSource = Path.Combine(pluginPath, "icon.svg") |> SvgSource.Load
            let svg = SvgImage(Source = svgSource)
            let icon = StarterIconSource(svg, svg)

            EverythingSearchEngine(icon, api), null

    override this.LoadDataTemplates() = null
