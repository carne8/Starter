namespace Starter.Features

open System
open Starter.Features
open Starter.SearchEngine
open Starter.TextMatching.FuzzyMatch

module SearchResult =
    // Returns a low value for a result that should be on top of the list
    let getWeight (resultScoreDb: IScoreDb) (fuzzyMatchResult: FuzzyResult voption) (sr: SearchResult) =
        let struct (usageScore, d) =
            sr.Id
            |> ValueOption.ofObj
            |> ValueOption.map resultScoreDb.GetResultScore
            |> ValueOption.defaultValue (struct (Int32.MaxValue, TimeSpan.MaxValue))

        let fuzzyMatchScore =
            match fuzzyMatchResult with
            | ValueSome fuzzyResult -> float fuzzyResult.Score
            | ValueNone -> 0.

        struct (
            sr.Priority,
            -(fuzzyMatchScore + (2. * usageScore)),
            d,
            sr.Name.Length,
            sr.Name
        )

module ContextMenuEntry =
    // Returns a low value for a result that should be on top of the list
    let getWeight (resultScoreDb: IScoreDb) (f: FuzzyResult voption) (r: IContextMenuEntry) =
        let struct (usageScore, d) =
            r.Id
            |> ValueOption.ofObj
            |> ValueOption.map resultScoreDb.GetResultScore
            |> ValueOption.defaultValue struct (Int32.MaxValue, TimeSpan.MaxValue)

        let fuzzyMatchScore =
            match f with
            | ValueSome fuzzyResult -> float fuzzyResult.Score
            | ValueNone -> 0.

        struct (
            ResultPriority.Static,
            -(fuzzyMatchScore + (2. * usageScore)),
            d,
            r.Name.Length,
            r.Name
        )
