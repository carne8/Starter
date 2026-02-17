namespace Starter.EverythingSearchEngine

open System.Collections.Generic
open System.Diagnostics
open System.Runtime.InteropServices
open System.Text
open System.Threading
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open R3
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


type EverythingSearchEngine(pluginPath, configDir, logger) =
    inherit DynamicSearchEngine(pluginPath, configDir, logger)

    let emptyResult = struct (Seq.empty, Observable.Empty())

    let pathStrBuilder = StringBuilder(300)

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
        task {
            let name =
                i
                |> Everything_GetResultFileName
                |> Marshal.PtrToStringUni

            pathStrBuilder.Clear() |> ignore
            Everything_GetResultFullPathName(i, pathStrBuilder, uint pathStrBuilder.Capacity)
            let path = pathStrBuilder.ToString()

            let icon = loadResultIcon path

            return { Name = name
                     Path = path
                     Icon = icon } :> ISearchResult
        }

    override this.Id = nameof EverythingSearchEngine
    override this.Name = "Everything"
    override this.ShortName = "Everything"
    override this.Icon = StarterIconSource.Empty
    override this.Activators = [| DefaultSearchEngineActivator(this) |]
    override this.ImportantResults = false
    override this.UseAsyncEnumerable = true

    override this.Search(_, _, _) = emptyResult
    override this.SearchAsync(query, _) =
        // Query Everything
        Everything_SetSearchW query |> ignore
        Everything_SetRequestFlags (RequestFlags.FILE_NAME ||| RequestFlags.PATH)
        Everything_SetMax 100u
        Everything_QueryW true |> ignore

        // Gather results
        let count = Everything_GetNumResults()
        logger.Verbose $"Request succeed: {count}"

        let enumerator (ct: CancellationToken) = // TODO: Compare with ResizeArray and Observable
            let mutable current = ValueNone
            let mutable i = 0u
            { new IAsyncEnumerator<ISearchResult> with
                member this.MoveNextAsync() =
                    match i < count && not ct.IsCancellationRequested with
                    | false -> ValueTask.FromResult false
                    | true ->
                        task {
                            let! result = readResult i
                            current <- ValueSome result
                            i <- i + 1u
                            return true
                        } |> ValueTask<bool>

                member this.Current = current |> ValueOption.get
                member this.DisposeAsync() = ValueTask.CompletedTask }

        { new IAsyncEnumerable<ISearchResult> with
            member this.GetAsyncEnumerator(ct) = enumerator ct }

    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? SearchResult as sr ->
            ProcessStartInfo(sr.Path, UseShellExecute = true)
            |> Process.Start
            |> function null -> () | d -> d.Dispose()
        | _ -> ()

    override this.LoadSettingsControl() = null
