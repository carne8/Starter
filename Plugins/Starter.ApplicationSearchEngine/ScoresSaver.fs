module Starter.ApplicationSearchEngine.ScoresSaver

open System
open System.Collections.Generic
open System.IO
open MemoryPack

[<MemoryPackable>]
type Scores = IDictionary<string, int * DateTimeOffset>

let writeToFile filePath (scores: IDictionary<string, int * DateTimeOffset>) =
    task {
        use file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write)
        do! MemoryPackSerializer.SerializeAsync<Scores>(file, scores)
    }

let readFromFile filePath =
    task {
        try
            use file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read)
            return! MemoryPackSerializer.DeserializeAsync<Scores> file
        with
        | _ -> return Dictionary<_, _>()
    }
