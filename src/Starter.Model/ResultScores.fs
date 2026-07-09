namespace Starter.Features

// App score based on https://github.com/ajeetdsouza/zoxide/wiki/Algorithm
open System
open System.IO
open System.Collections.Generic
open System.Threading.Tasks
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
type private ScoreData = IDictionary<string, ScoreDbEntry>

type IScoreDb =
    abstract member SaveToFile: filePath: string -> Task
    abstract member RunMaxAgingPolicy: unit -> unit
    abstract member GetResultScore: resultId: string -> struct (float * TimeSpan)
    abstract member IncreaseResultScore: resultId: string -> unit

type ScoreDb(scores: ScoreData, maxAge: float) =
    static member ReadFromFile filePath maxAge =
        match filePath |> File.Exists with
        | false -> ScoreDb(Dictionary(), maxAge)
        | true ->
            let bytes = filePath |> File.ReadAllBytes
            let scores =
                try MemoryPackSerializer.Deserialize<ScoreData> bytes
                with _ -> null

            match scores with
            | null -> ScoreDb(Dictionary(), maxAge)
            | scores -> ScoreDb(scores, maxAge)

    interface IScoreDb with
        member _.SaveToFile filePath =
            task {
                // Create directory if it doesn't exist
                if filePath |> File.Exists |> not then
                    match filePath |> Path.GetDirectoryName with
                    | null -> failwith "Invalid file path"
                    | fileDir -> fileDir |> Directory.CreateDirectory |> ignore

                use file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write)
                do! MemoryPackSerializer.SerializeAsync<ScoreData>(file, scores)
            }

        // Remove excessive entries from the database
        // Behaviour documented (and copied) here: https://github.com/ajeetdsouza/zoxide/wiki/Algorithm#aging
        member _.RunMaxAgingPolicy() =
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
        member _.GetResultScore resultId =
            match scores.TryGetValue resultId with
            | false, _ -> struct (0., TimeSpan.MaxValue)
            | true, resultScore -> resultScore |> ScoreDbEntry.computeScore

        /// Increase app score in the database
        member _.IncreaseResultScore (resultId: string) =
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
