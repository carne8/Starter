[<AutoOpen>]
module Helpers

// TODO: Benchmark for the fun
type OrElseBuilder() =
    member inline _.Return x = ValueSome x
    member inline _.ReturnFrom x = x

    member inline _.Combine(a,b) =
        match a with
        | Some _ -> a
        | None -> b()

    member inline _.Combine(a,b) =
        match a with
        | ValueSome _ -> a
        | ValueNone -> b()

    member inline _.Zero() = ValueNone
    member inline _.Delay f = f
    member inline _.Run f = f()

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

    let tryPickV chooser (s: _ seq) =
        let e = s.GetEnumerator()
        let rec loop () =
            if e.MoveNext()  then
                match chooser e.Current with
                | ValueSome e -> ValueSome e
                | ValueNone -> loop ()
            else
                ValueNone

        loop ()

    let inline choosev f =
        Seq.choose (f >> Option.ofValueOption)

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

type System.String with
    member inline this.TryIndexOf(s: string) =
        match this.IndexOf s with
        | -1 -> ValueNone
        | n -> ValueSome n

    member inline this.TryIndexOf(c: char) =
        match this.IndexOf c with
        | -1 -> ValueNone
        | n -> ValueSome n
