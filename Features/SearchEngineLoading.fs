module Starter.Features.SearchEngineLoading

open System
open System.IO
open System.Reflection
open System.Runtime.Loader

open Starter.SearchEngine
open FsToolkit.ErrorHandling

type private SearchEngineLoadContext(dllPath) =
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
let private loadAssembly relativePath =
    let root =
        AppContext.BaseDirectory
        |> Path.GetDirectoryName
        |> Option.ofNull
        |> Option.bind (Path.GetDirectoryName >> Option.ofNull)
        |> Option.bind (Path.GetDirectoryName >> Option.ofNull)
        |> Option.bind (Path.GetDirectoryName >> Option.ofNull)

    root |> Option.map (fun root ->
        let assemblyPath = Path.Combine(root, relativePath) |> Path.GetFullPath
        let assemblyDir =
            match assemblyPath |> Path.GetDirectoryName with
            | null -> failwith "Incorrect assembly directory"
            | dir -> dir

        let loadContext = SearchEngineLoadContext assemblyPath

        assemblyDir,
        assemblyPath
        |> AssemblyName.GetAssemblyName
        |> loadContext.LoadFromAssemblyName
    )

/// Loads search engines from an assembly
let private loadAssemblySearchEngines<'SearchEngineKind> (libDir: string, assembly: Assembly) =
    let expectedType = typeof<'SearchEngineKind>

    assembly.GetTypes()
    |> Array.choose (fun type' ->
        if expectedType.IsAssignableFrom type' then
            let libDirectory = libDir |> Path.GetDirectoryName

            match Activator.CreateInstance(type', libDirectory) with
            | null -> None
            | searchEngine ->
                searchEngine
                |> unbox<'SearchEngineKind>
                |> Some
        else
            None
    )

/// Load all search engines in a directory (not recursive)
let loadSearchEngineFromDirectory directoryPath =
    let assemblies =
        Directory.GetFiles(directoryPath, "*SearchEngine.dll")
        |> Array.choose loadAssembly

    assemblies |> Array.collect loadAssemblySearchEngines<StaticSearchEngine>,
    assemblies |> Array.collect loadAssemblySearchEngines<DynamicSearchEngine>
