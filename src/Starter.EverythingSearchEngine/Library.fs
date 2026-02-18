namespace Starter.EverythingSearchEngine

open System.Diagnostics
open System.Runtime.InteropServices
open System.Text
open System.Threading.Tasks
open R3
open Serilog
open Starter.EverythingSearchEngine
open Starter.SearchEngine
open Vanara

open EverythingAPI
open IconHelper
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


type EverythingSearchEngine(logger: ILogger) =
    let pathStrBuilder = StringBuilder(300)
    let [<Literal>] maxResultsCount = 300u

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
        with _ -> StarterIconSource.Empty

    let readResult i =
        let name =
            i
            |> Everything_GetResultFileName
            |> Marshal.PtrToStringUni

        match name with
        | null -> ValueNone
        | name ->
            pathStrBuilder.Clear() |> ignore
            Everything_GetResultFullPathName(i, pathStrBuilder, uint pathStrBuilder.Capacity)
            let path = pathStrBuilder.ToString()

            let icon = loadResultIcon path

            { Name = name
              Path = path
              Icon = icon }
            :> ISearchResult
            |> ValueSome

    interface IDynamicSearchEngine with
        member this.Id = nameof EverythingSearchEngine
        member this.Name = "Everything"
        member this.ShortName = "Everything"
        member this.Icon = StarterIconSource.Empty
        member this.Activators = [| DefaultSearchEngineActivator(this) |]
        member this.ImportantResults = false
        member this.BufferResults = true

        member this.Search(query, ct, _) =
            // Query Everything
            Everything_SetSearchW query |> ignore
            Everything_SetRequestFlags (RequestFlags.FILE_NAME ||| RequestFlags.PATH)
            Everything_SetMax maxResultsCount
            Everything_QueryW true |> ignore

            // Gather results
            let count = Everything_GetNumResults()
            logger.Verbose $"Request succeed: {count}"

            Task.Run<unit>(fun () -> task {
                let mutable i = 0u
                while i < count && not ct.IsCancellationRequested do
                    i
                    |> readResult
                    |> ValueOption.iter (Seq.singleton >> resultsObservable.OnNext)
                    i <- i + 1u
            }) |> ignore

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

    override this.LoadSearchEngineIds() = [| nameof EverythingSearchEngine |]
    override this.LoadSearchEngine(_, _, logger) = EverythingSearchEngine logger, null
