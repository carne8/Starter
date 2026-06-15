module Starter.TextMatching.FuzzyMatch

// Reimplementation of the fzf algorithm
// https://github.com/junegunn/fzf/blob/master/src/algo/algo.go

open System
open System.Text

open Starter.TextMatching.Text
open Starter.TextMatching.TextNormalization
open Starter.TextMatching.Constants
open Starter.TextMatching.Memory

[<Struct>]
type FuzzyResult =
    { Start: int
      End: int
      Score: int16
      MatchingPositions: bool array | null }

#if DEBUG
let private debug (T: int Span) (pattern: Rune array) (F: int32 Span) (lastIdx: int) (H: int16 Span) (C: int16 Span) =
    let width = lastIdx - int F[0] + 1

    for i in 0 .. pattern.Length - 1 do
        let f = int F[i]
        let I = i * width

        if i = 0 then
            printf "  "
            for j in f .. lastIdx do
                printf " %s " (T[j] |> Rune |> string)
            printfn ""

        printf "%s " (string pattern[i])

        for idx in int F[0] .. f - 1 do
            printf " 0 "

        for idx in f .. lastIdx do
            let h = H[i * width + idx - int F[0]]
            printf "%2d " h
        printfn ""

        printf "  "
        for idx = 0 to width - 1 do
            let col = idx + int F[0]
            let p =
                if col < int F[i] then 0s
                else C[I + idx]

            if p > 0s then
                printf "%2d " p
            else
                printf "   "
        printfn ""
#endif

let private posArray withPos (length: int) : bool array | null =
    if withPos then
        Array.zeroCreate<bool> length
    else
        null

let runes caseSensitive normalize withPos (slab: Slab) (pattern: Rune array) (input: Rune Span) =
    // Assume that pattern is given in lowercase if case-insensitive.
    // First check if there's a match and calculate bonus for each position.
    // If the input string is too long, consider finding the matching chars in
    // this phase as well (non-optimal alignment).
    let M = pattern.Length
    if M = 0 then
        ValueSome { Start = 0; End = 0; Score = 0s; MatchingPositions = null }
    else

    let N = input.Length
    if M > N then
        ValueNone
    else

    // Since O(nm) algorithm can be prohibitively expensive for large input,
    // we fall back to the greedy algorithm.
    if N*M > slab.i16.Length then
        ValueNone // TODO: fallback on a faster algorithm
    else

    // TODO: build phase 1
    // Phase 1. Optimized search for ASCII string
    let minIdx, maxIdx = 0, input.Length

    // Reuse pre-allocated integer slice to avoid unnecessary sweeping of garbages
    let mutable offset16 = 0
    let mutable offset32 = 0
    let mutable H0 = slab.i16.AsSpan(offset16, N)
    offset16 <- offset16 + N

    let mutable C0 = slab.i16.AsSpan(offset16, N)
    offset16 <- offset16 + N

    // Bonus point for each position
    let mutable B = slab.i16.AsSpan(offset16, N)
    offset16 <- offset16 + N

    // The first occurrence of each character in the pattern
    let mutable F = slab.i32.AsSpan(offset32, M)
    offset32 <- offset32 + M

    // Rune array
    let mutable T = slab.i32.AsSpan(offset32, N)
    offset32 <- offset32 + N
    for i = 0 to input.Length-1 do // Copy runes
        T[i] <- input[i].Value

    // Phase 2: Calculate bonus for each point
    let mutable maxScore, maxScorePos = 0s, 0
    let mutable pidx, lastIdx = 0, 0
    let mutable pchar0, pchar, prevH0, prevClass, inGap = pattern[0], pattern[0], 0s, CharClass.initialCharClass, false

    let mutable off = 0
    let mutable finished = false
    while off <= N-1 && not finished do
        let mutable charCodePoint = T[off]
        let mutable charClass = CharClass.White

        if charCodePoint |> Rune.isAscii then
            charClass <- CharClass.asciiCharClasses[charCodePoint]
            if not caseSensitive && charClass = CharClass.Upper then
                charCodePoint <- charCodePoint + 32
                T[off] <- charCodePoint
        else
            charClass <- charCodePoint |> CharClass.ofNonAscii
            if not caseSensitive && charClass = CharClass.Upper then
                charCodePoint <- charCodePoint |> Rune.toLower

            if normalize then
                charCodePoint <- Rune.normalize charCodePoint

            T[off] <- charCodePoint

        let bonus = Bonus.matrix[int prevClass, int charClass]
        B[off] <- bonus
        prevClass <- charClass

        if charCodePoint = pchar.Value then
            if pidx < M then
                F[pidx] <- off
                pidx <- pidx+1
                pchar <- pattern[min pidx (M-1)]
            lastIdx <- off

        if charCodePoint = pchar0.Value then
            let score = Score.match' + bonus*Bonus.firstCharMultiplier
            H0[off] <- score
            C0[off] <- 1s

            if M = 1 && score > maxScore then
                maxScore <- score
                maxScorePos <- off
                if bonus >= Bonus.boundary then
                    finished <- true

            inGap <- false
        else
            H0[off] <-
                if inGap then
                    max (prevH0 + Score.gapExtension) 0s
                else
                    max (prevH0 + Score.gapStart) 0s

            C0[off] <- 0s
            inGap <- true

        prevH0 <- H0[off] // TODO: Benchmark
        off <- off+1

    if pidx <> M then // Input doesn't contain pattern
        ValueNone
    else

    if M = 1 then
        if withPos then
            ValueSome { Start = minIdx + maxScorePos
                        End = minIdx + maxScorePos + 1
                        Score = maxScore
                        MatchingPositions = Array.init N (fun idx -> idx = minIdx + maxScorePos) }
        else
            ValueSome { Start = minIdx + maxScorePos
                        End = minIdx + maxScorePos
                        Score = maxScore
                        MatchingPositions = null }
    else

    // Phase 3: Fill in score matrix
    // Unlike the original algorithm, we do not allow omission.
    let f0 = F[0]
    let width = lastIdx - f0 + 1

    let mutable H = slab.i16.AsSpan(offset16, width*M)
    offset16 <- offset16 + width*M
    H0.Slice(f0, width).CopyTo H

    // Possible length of consecutive chunk at each position.
    let mutable C = slab.i16.AsSpan(offset16, width*M)
    C0.Slice(f0, width).CopyTo C

    let FSub = F.Slice(1, F.Length - 1)
    let PSub = pattern.AsSpan(1, FSub.Length)
    for off = 0 to FSub.Length - 1 do
        let f = FSub[off]
        let pchar = PSub[off]
        let pidx = off + 1
        let row = pidx * width
        let mutable inGap = false

        let TSub = T.Slice(f, lastIdx+1 - f)
        let BSub = B.Slice(f).Slice(0, TSub.Length)
        let mutable CSub = C.Slice(row+f-f0).Slice(0, TSub.Length)
        let CDiag = C.Slice(row+f-f0-1-width).Slice(0, TSub.Length)
        let mutable HSub = H.Slice(row+f-f0).Slice(0, TSub.Length)
        let HDiag = H.Slice(row+f-f0-1-width).Slice(0, TSub.Length)
        let mutable HLeft = H.Slice(row+f-f0-1).Slice(0, TSub.Length)
        HLeft[0] <- 0s

        for off = 0 to TSub.Length - 1 do
            let c = TSub[off]
            let col = off + f
            let mutable consecutive = 0s

            let s2 =
                if inGap then
                    HLeft[off] + Score.gapExtension
                else
                    HLeft[off] + Score.gapStart

            let s1 =
                if pchar.Value = c then
                    let score = HDiag[off] + Score.match'
                    let mutable b = BSub[off]
                    consecutive <- CDiag[off] + 1s
                    if consecutive > 1s then
                        let fb = B[col - int consecutive + 1]
                        // Break consecutive chunk
                        if b >= Bonus.boundary && b > fb then
                            consecutive <- 1s
                        else
                            b <- max b (max Bonus.consecutive fb)

                    if score+b < s2 then
                        consecutive <- 0s
                        score + BSub[off]
                    else
                        score + b
                else
                    0s

            CSub[off] <- consecutive
            inGap <- s1 < s2
            let score = max (max s1 s2) 0s
            if pidx = M-1 && score > maxScore then
                maxScore <- score
                maxScorePos <- col
            HSub[off] <- score

    // #if DEBUG && !FABLE_COMPILER
    // debug T pattern F lastIdx H C
    // #endif

    // Phase 4: Backtrace to find the character positions
    let mutable j = f0
    let matchingPositions = posArray withPos N
    match matchingPositions with
    | null -> ()
    | matchingPositions when withPos ->
        if withPos then
            let mutable i = M-1
            j <- maxScorePos
            let mutable preferMatch = true
            let mutable finished = false

            while not finished do
                let I = i * width
                let j0 = j - f0
                let s = H[I+j0]

                let s1 =
                    if i > 0 && j >= F[i] then
                        H[I-width+j0-1]
                    else
                        0s

                let s2 =
                    if j > F[i] then
                        H[I+j0-1]
                    else
                        0s

                if s > s1 && (s > s2 || s = s2 && preferMatch) then
                    matchingPositions[j+minIdx] <- true
                    if i = 0 then
                        j <- j+1 // To cancel the `j <- j-1`
                        finished <- true
                    i <- i-1

                preferMatch <- C[I+j0] > 1s || I+width+j0+1 < C.Length && C[I+width+j0+1] > 0s
                j <- j-1
    | _ -> ()

    // Start offset we return here is only relevant when begin tiebreak is used.
    // However, finding the accurate offset requires backtracking, and we don't
    // want to pay extra cost for the option that has lost its importance.
    ValueSome { Start = minIdx + j
                End = minIdx + maxScorePos + 1
                Score = maxScore
                MatchingPositions = matchingPositions }

#nowarn 9
open FSharp.NativeInterop

let string caseSensitive normalize withPos slab pattern (input: string) =
    let span =
        Span<Rune>(
            input.Length
            |> NativePtr.stackalloc<Rune>
            |> NativePtr.toVoidPtr,
            input.Length
        )

    let mutable enumerator = input.EnumerateRunes()
    let mutable i = 0
    while enumerator.MoveNext() do
        span[i] <- enumerator.Current
        i <- i+1

    runes caseSensitive normalize withPos slab pattern (span.Slice(0, i))
