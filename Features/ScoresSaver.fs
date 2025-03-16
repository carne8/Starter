module Starter.Features.ScoresSaver

open System
open System.IO
open System.Collections.Generic
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
            let! scores = MemoryPackSerializer.DeserializeAsync<Scores> file

            match scores with
            | null -> return Dictionary() :> IDictionary<_, _>
            | scores -> return scores

        with _ ->
            return Dictionary()
    }
