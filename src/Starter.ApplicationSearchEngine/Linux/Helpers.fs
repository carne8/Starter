[<AutoOpen>]
module Helpers

// TODO: Benchmark for the fun
type OrElseBuilder() =
    member _.Return x = ValueSome x
    member _.ReturnFrom x = x

    member _.Combine(a,b) =
        match a with
        | Some _ -> a
        | None -> b()

    member _.Combine(a,b) =
        match a with
        | ValueSome _ -> a
        | ValueNone -> b()

    member _.Zero() = ValueNone
    member _.Delay f = f
    member _.Run f = f()

let orElse = OrElseBuilder()

[<RequireQualifiedAccess>]
module Array =
    let tryPickV chooser (array: _ array | null) =
        match array with
        | null -> nullArg "array"
        | array ->
            let rec loop i =
                if i >= array.Length then
                    ValueNone
                else
                    match chooser array[i] with
                    | ValueNone -> loop (i + 1)
                    | res -> res

            loop 0

[<RequireQualifiedAccess>]
module Seq =
    let inline private checkNonNull argName arg =
        if isNull arg then
            nullArg argName

    let tryHeadV (source: seq<_> | null) =
        match source with
        | null -> nullArg "source"
        | source ->
            use e = source.GetEnumerator()
            if e.MoveNext() then
                ValueSome e.Current
            else
                ValueNone

[<RequireQualifiedAccess>]
module Task =
    open System.Threading.Tasks

    let catch onError (t: _ Task) =
        task {
            try
                return! t
            with e ->
                return onError e
        }

[<RequireQualifiedAccess>]
module Result =
    let inline requireNotNull (error: 'error) (value: 'ok | null) : Result<'ok, 'error> =
        match value with
        | null -> Error error
        | nonnull -> Ok nonnull
