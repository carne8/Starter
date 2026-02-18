module Starter.Features.SearchEngineLoading

open System
open System.IO
open System.Reflection
open System.Runtime.Loader

open FsToolkit.ErrorHandling
open Starter.Features.Logging
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

/// Loads search engine factories from an assembly
let private loadAssemblyFactories (assemblyDir: string, assembly: Assembly) =
    assembly.GetTypes() |> Array.choose (fun type' ->
        if typeof<SearchEngineFactory>.IsAssignableFrom type' then
            Activator.CreateInstance(type', assemblyDir)
            |> Option.ofObj
            |> Option.map unbox<SearchEngineFactory>
        else
            None
    )

let loadSearchEnginesFromFactory (factory: SearchEngineFactory) =
    factory.LoadSearchEngineIds() |> Seq.choose (fun id ->
        match id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 with
        | false ->
            logger.Error $"Invalid search engine id: {id}"
            None
        | true ->
            try
                factory.LoadSearchEngine(
                    id,
                    Constants.PluginConfigDirectory id,
                    logger.ForContext("Context", id)
                )
                |> Some
            with exn ->
                logger.Error(exn, "Failed to create search engine.")
                None
    )

/// Load all search engines in a directory (not recursive)
let loadFactoriesFromDirectory directoryPath =
    Directory.GetFiles(Path.GetFullPath(directoryPath), "*SearchEngine.dll")
    |> Seq.collect (fun dir ->
        try
            dir
            |> loadAssembly
            |> loadAssemblyFactories
        with exn ->
            logger.Error(exn, "Failed to load assembly.")
            Array.empty
    )


// /// Precompile methods for a dynamic search engine
// let prepareSearchEngine (se: DynamicSearchEngine) =
//     se.GetType()
//       .GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
//     |> Array.iter (_.MethodHandle >> RuntimeHelpers.PrepareMethod)
