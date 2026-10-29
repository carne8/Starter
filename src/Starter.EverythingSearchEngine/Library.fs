namespace Starter.EverythingSearchEngine

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open System.Text
open System.Threading
open System.Threading.Tasks

open Avalonia.Threading
open EverythingAPI
open Helpers
open Starter.SearchEngine
open Starter.Shared
open Starter.Shared.IconHelper

open Avalonia.Svg.Skia
open R3
open Vanara
open Vanara.Windows.Shell

type EverythingSearchResult(
        Name: string,
        Path: string,
        Icon: StarterIconSource
    ) =
    inherit SearchResult()

    member this.Path = Path
    override this.Id = $"everything:{Path}"
    override this.Name = Name
    override this.Description = Path
    override this.Keywords = null
    override this.Icon = Icon
    override this.Priority = ResultPriority.Search
    override this.ShowIfNoActivator = true
    override this.ActivatorFilter = Array.empty
    override this.ContextMenuLoader = new ContextMenu.ContextMenuLoader(Path)

type EverythingSearchEngine(icon, api: IEverything) =
    inherit DynamicSearchEngine()

    let pathStrBuilder = StringBuilder(300)

    let loadResultIcon (path: string) =
        if path.Contains "$RECYCLE.BIN\\" then icon else
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

            EverythingSearchResult(name, path, icon)
            :> SearchResult
            |> ValueSome

    let queryEverything (ct: CancellationToken) maxResultsCount query : Observable<_> =
        let results = new Subject<_>()

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
                |> ValueOption.iter (Seq.singleton >> results.OnNext)
                i <- i + 1u

            results.OnCompleted()
        }) |> ignore

        results

    override this.Id = nameof EverythingSearchEngine
    override this.Name = "Everything"
    override this.Icon = icon
    override this.Activators = [| DefaultSearchEngineActivator(this) |]
    override this.BufferResults = true

    override this.Search(query, ct, activator) =
        let results =
            if activator <> null then
                queryEverything ct 300u query
            elif query |> String.IsNullOrWhiteSpace |> not then
                queryEverything ct 30u query
            else
                Observable.Empty()

        Seq.empty, results

    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? EverythingSearchResult as sr ->
            ProcessStartInfo(sr.Path, UseShellExecute = true)
            |> Process.Start
            |> function null -> () | d -> d.Dispose()
        | _ -> ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    let api =
        match RuntimeInformation.ProcessArchitecture with
        | Architecture.X86 -> Everything32() :> IEverything |> ValueSome
        | Architecture.X64 -> Everything64() :> IEverything |> ValueSome
        | Architecture.Arm64 -> EverythingArm64() :> IEverything |> ValueSome
        | _ -> ValueNone

    override this.LoadSearchEngineIds() =
        if OperatingSystem.IsWindows() then
            [| nameof EverythingSearchEngine |]
        else
            Array.empty

    override this.LoadSearchEngine(_, _, _, _) =
        match api with
        | ValueNone ->
            "No Everything library can be loaded for the current process architecture"
            |> PlatformNotSupportedException
            |> raise
        | ValueSome api ->
            let svgSource = Path.Combine(pluginPath, "icon.svg") |> SvgSource.Load
            let svg = Dispatcher.UIThread.Invoke(fun () -> SvgImage(Source = svgSource))
            let icon = StarterIconSource(svg, svg)

            EverythingSearchEngine(icon, api), null
