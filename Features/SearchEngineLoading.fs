module Starter.Features.SearchEngineLoading

open System
open System.IO
open System.Reflection
open System.Runtime.Loader

open Starter.SearchEngine
open FsToolkit.ErrorHandling

type SearchEngineLoadContext(dllPath) =
    inherit AssemblyLoadContext()

    let resolver = AssemblyDependencyResolver dllPath

    let isSharedAssembly (assemblyName: AssemblyName) =
        match assemblyName.Name with
        | null -> false
        | assemblyName ->
            Constants.SharedAssemblies |> Seq.contains assemblyName

    override this.Load(assemblyName: AssemblyName): Assembly | null =
        match assemblyName |> isSharedAssembly with
        | true -> Assembly.Load assemblyName
        | false ->
            let assemblyPath = resolver.ResolveAssemblyToPath assemblyName
            match assemblyPath with
            | null -> null
            | assemblyPath -> this.LoadFromAssemblyPath assemblyPath

    override this.LoadUnmanagedDll(unmanagedDllName) =
        let libraryPath = resolver.ResolveUnmanagedDllToPath unmanagedDllName
        match libraryPath with
        | null -> IntPtr.Zero
        | libraryPath -> this.LoadUnmanagedDllFromPath libraryPath

/// Loads an assembly
let private loadSearchEngineAssembly relativePath =
    let root =
        AppContext.BaseDirectory
        |> Path.GetDirectoryName
        |> Option.ofNull
        |> Option.bind (Path.GetDirectoryName >> Option.ofNull)
        |> Option.bind (Path.GetDirectoryName >> Option.ofNull)
        |> Option.bind (Path.GetDirectoryName >> Option.ofNull)

    root |> Option.map (fun root ->
        let path = Path.Combine(root, relativePath) |> Path.GetFullPath

        let loadContext = SearchEngineLoadContext path
        path
        |> AssemblyName.GetAssemblyName
        |> loadContext.LoadFromAssemblyName
    )

/// Loads search engines in an assembly
let private loadAssemblySearchEngines (libPath: string) (assembly: Assembly) =
    let interfaceType = typeof<SearchEngine>

    assembly.GetTypes()
    |> Array.choose (fun type' ->
        if interfaceType.IsAssignableFrom type' then
            let libDirectory = libPath |> Path.GetDirectoryName

            match Activator.CreateInstance(type', libDirectory) with
            | null -> None
            | searchEngine ->
                searchEngine
                :?> SearchEngine
                |> Some
        else
            None
    )

/// Loads search engines from a DLL
let loadSearchEngines libPath =
    loadSearchEngineAssembly libPath
    |> Option.map (loadAssemblySearchEngines libPath)
    |> Option.defaultValue Array.empty
