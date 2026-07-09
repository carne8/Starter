module internal Starter.TextMatching.Text

open System.Text

/// highest ASCII code point
let [<Literal>] private MaxAscii = 127
let private delimiterRunes = "/,:;|".EnumerateRunes() |> Seq.toArray |> Array.map _.Value
let private whiteRunes = " \t\n\v\f\r\x85\xA0".EnumerateRunes() |> Seq.toArray |> Array.map _.Value

module Rune =
    let inline isAscii (codePoint: int) = codePoint <= MaxAscii
    let inline toLower (codePoint: int) = Rune codePoint |> Rune.ToLowerInvariant |> _.Value

type CharClass =
    | White = 0
    | NonWord = 1
    | Delimiter = 2
    | Lower = 3
    | Upper = 4
    | Letter = 5
    | Number = 6

module CharClass =
    let [<Literal>] initialCharClass = CharClass.White

    let asciiCharClasses =
        Array.init<CharClass> (MaxAscii + 1) (fun idx ->
            let c = char idx

            if c >= 'a' && c <= 'z' then
                CharClass.Lower
            elif c >= 'A' && c <= 'Z' then
                CharClass.Upper
            elif c >= '0' && c <= '9' then
                CharClass.Number
            elif whiteRunes |> Array.contains idx then
                CharClass.White
            elif delimiterRunes |> Array.contains idx then
                CharClass.Delimiter
            else
                CharClass.NonWord
        )

    let inline ofAscii c = asciiCharClasses[c]
    let inline ofNonAscii (charInt: int) =
        let rune = Rune charInt
        if Rune.IsLower rune then CharClass.Lower
        elif Rune.IsUpper rune then CharClass.Upper
        elif Rune.IsNumber rune then CharClass.Number
        elif Rune.IsLetter rune then CharClass.Letter
        elif Rune.IsWhiteSpace rune then CharClass.White
        elif delimiterRunes |> Array.contains rune.Value then CharClass.Delimiter
        else CharClass.NonWord
