module Starter.Features.SearchEngineLoading

open System
open System.IO
open System.Reflection
open System.Runtime.Loader

open FsToolkit.ErrorHandling
open Starter.Features.Logging
open Starter.SearchEngine

let private logger = logger.ForContext("Context", "Starter/EngineLoading")

type private SearchEngineLoadContext(dllPath) =
    inherit AssemblyLoadContext()

    let resolver = AssemblyDependencyResolver dllPath

    override this.Load(assemblyName: AssemblyName): Assembly | null =
        try
            Assembly.Load assemblyName
        with _ ->
            let assemblyPath = resolver.ResolveAssemblyToPath assemblyName
            match assemblyPath with
            | null -> null
            | assemblyPath -> this.LoadFromAssemblyPath assemblyPath

    override this.LoadUnmanagedDll unmanagedDllName =
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

let loadSearchEnginesFromFactory clipboard (factory: SearchEngineFactory) =
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
                    logger.ForContext("Context", id),
                    clipboard
                )
                |> Some
            with exn ->
                logger.Error(exn, "Failed to create search engine.")
                None
    )

/// Load all search engines in a directory (not recursive)
let loadFactoriesFromDirectory directoryPath =
    let files =
        try
            Directory.GetFiles(Path.GetFullPath(directoryPath), "*.deps.json")
            |> Array.choose (fun dependenciesFile ->
                let dllFile =
                    dependenciesFile.Substring(0, dependenciesFile.Length - ".deps.json".Length)
                    + ".dll"

                if dllFile |> File.Exists then
                    Some dllFile
                else
                    None
            )
        with e ->
            logger.Warning $"Failed to load factories in directory {directoryPath}: {e.Message}"
            Array.empty

    files |> Seq.collect (fun assemblyPath ->
        try
            let factories =
                assemblyPath
                |> loadAssembly
                |> loadAssemblyFactories

            if Array.isEmpty factories then
                logger.Warning("Assembly {AssemblyPath} does not contain factories", assemblyPath)

            factories
        with exn ->
            logger.Warning(exn, "Failed to load assembly at {AssemblyPath}", assemblyPath)
            Array.empty
    )


// /// Precompile methods for a dynamic search engine
// let prepareSearchEngine (se: DynamicSearchEngine) =
//     se.GetType()
//       .GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
//     |> Array.iter (_.MethodHandle >> RuntimeHelpers.PrepareMethod)
