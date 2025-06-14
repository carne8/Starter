module Starter.Features.SearchEngineLoading

open System
open System.IO
open System.Reflection
open System.Runtime.Loader

open Starter.SearchEngine

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
let private loadAssembly (assemblyPath: string) =
    let assemblyDir =
        match assemblyPath |> Path.GetDirectoryName with
        | null -> failwith "Incorrect assembly directory"
        | dir -> dir

    let loadContext = SearchEngineLoadContext assemblyPath

    assemblyDir,
    assemblyPath
    |> AssemblyName.GetAssemblyName
    |> loadContext.LoadFromAssemblyName

/// Loads search engines from an assembly
let private loadAssemblySearchEngines<'SearchEngineKind> (assemblyDir: string, assembly: Assembly) =
    let expectedType = typeof<'SearchEngineKind>

    assembly.GetTypes()
    |> Array.choose (fun type' ->
        if expectedType.IsAssignableFrom type' then
            let logger = Logging.logger.ForContext("Context", expectedType.Name)

            match Activator.CreateInstance(type', assemblyDir, logger) with
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
        Directory.GetFiles(Path.GetFullPath(directoryPath), "*SearchEngine.dll")
        |> Array.map loadAssembly

    assemblies |> Array.collect loadAssemblySearchEngines<StaticSearchEngine>,
    assemblies |> Array.collect loadAssemblySearchEngines<DynamicSearchEngine>

// /// Precompile methods for a dynamic search engine
// let prepareSearchEngine (se: DynamicSearchEngine) =
//     se.GetType()
//       .GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
//     |> Array.iter (_.MethodHandle >> RuntimeHelpers.PrepareMethod)
