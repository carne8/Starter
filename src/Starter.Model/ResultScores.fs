namespace Starter.Features

// App score based on https://github.com/ajeetdsouza/zoxide/wiki/Algorithm
open System
open System.IO
open System.Collections.Generic
open MemoryPack

[<Struct; MemoryPackable>]
type ScoreDbEntry =
    { AccessCount: int
      LastAccessTime: DateTimeOffset }

    static member computeScore (scoreDbEntry: ScoreDbEntry) =
        let d = DateTimeOffset.Now - scoreDbEntry.LastAccessTime
        let s = float scoreDbEntry.AccessCount

        let f =
            if d.TotalHours < 1 then s * 4.
            elif d.TotalDays < 1 then s * 2.
            elif d.TotalDays < 7 then s / 2.
            else s / 4.

        struct (f, d)

[<MemoryPackable>]
type ScoreDb = IDictionary<string, ScoreDbEntry>

module ScoreDb =
    let writeToFile filePath (scores: ScoreDb) =
        task {
            // Create directory if it doesn't exist
            if filePath |> File.Exists |> not then
                match filePath |> Path.GetDirectoryName with
                | null -> failwith "Invalid file path"
                | fileDir -> fileDir |> Directory.CreateDirectory |> ignore

            use file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write)
            do! MemoryPackSerializer.SerializeAsync<ScoreDb>(file, scores)
        }

    let readFromFile filePath =
        match filePath |> File.Exists with
        | false -> Dictionary() :> IDictionary<_, _>
        | true ->
            let bytes = filePath |> File.ReadAllBytes
            let scores =
                try MemoryPackSerializer.Deserialize<ScoreDb> bytes
                with _ -> null

            match scores with
            | null -> Dictionary() :> IDictionary<_, _>
            | scores -> scores

    // Remove excessive entries from the database
    // Behaviour documented (and copied) here: https://github.com/ajeetdsouza/zoxide/wiki/Algorithm#aging
    let runMaxAgingPolicy maxAge (scores: ScoreDb) =
        let totalScore =
            scores
            |> Seq.sumBy (_.Value >> _.AccessCount)
            |> float

        if totalScore > maxAge then
            let k = (0.9 * maxAge) / totalScore

            for kv in scores do
                let resultScore = kv.Value
                let newScore = float resultScore.AccessCount * k |> Math.Round |> int

                match newScore with
                | 0 -> scores.Remove kv.Key |> ignore
                | _ ->
                    scores[kv.Key] <-
                        { AccessCount = newScore
                          LastAccessTime = resultScore.LastAccessTime }

    /// Returns useful data for sorting results
    let getResultScore (scores: ScoreDb) resultId =
        match scores.TryGetValue resultId with
        | false, _ -> struct (0., TimeSpan.MaxValue)
        | true, resultScore -> resultScore |> ScoreDbEntry.computeScore

    /// Increase app score in the database
    let increaseAppScore (resultId: string) (scores: ScoreDb) =
        match scores.TryGetValue resultId with
        | true, prevResultScore ->
            scores[resultId] <-
                { AccessCount = prevResultScore.AccessCount + 1
                  LastAccessTime = DateTimeOffset.Now }
        | false, _ ->
            scores.Add(
                resultId,
                { AccessCount = 1
                  LastAccessTime = DateTimeOffset.Now }
            )
