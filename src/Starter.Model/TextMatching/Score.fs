module Starter.TextMatching.Constants

open Text

module internal Score =
    let [<Literal>] match' = 16s
    let [<Literal>] gapStart = -3s
    let [<Literal>] gapExtension = -1s

module internal Bonus =
    // We prefer matches at the beginning of a word, but the bonus should not be
    // too great to prevent the longer acronym matches from always winning over
    // shorter fuzzy matches. The bonus point here was specifically chosen that
    // the bonus is cancelled when the gap between the acronyms grows over
    // 8 characters, which is approximately the average length of the words found
    // in web2 dictionary and my file system.
    let [<Literal>] boundary = Score.match' / 2s


    // Although bonus point for non-word characters is non-contextual, we need it
    // for computing bonus points for consecutive chunks starting with a non-word
    // character.
    let [<Literal>] nonWord = Score.match' / 2s

    // Edge-triggered bonus for matches in camelCase words.
    // Compared to word-boundary case, they don't accompany single-character gaps
    // (e.g. FooBar vs. foo-bar), so we deduct bonus point accordingly.
    let [<Literal>] camel123 = boundary + Score.gapExtension

    // Minimum bonus point given to characters in consecutive chunks.
    // Note that bonus points for consecutive matches shouldn't have needed if we
    // used fixed match score as in the original algorithm.
    let [<Literal>] consecutive = -(Score.gapStart + Score.gapExtension)

    // The first character in the typed pattern usually has more significance
    // than the rest so it's important that it appears at special positions where
    // bonus points are given, e.g. "to-go" vs. "ongoing" on "og" or on "ogo".
    // The amount of the extra bonus should be limited so that the gap penalty is
    // still respected.
    let [<Literal>] firstCharMultiplier = 2s

    // Extra bonus for word boundary after whitespace character or beginning of the string
    let [<Literal>] bonusBoundaryWhite = boundary + 2s

    // Extra bonus for word boundary after slash, colon, semicolon, and comma
    let [<Literal>] bonusBoundaryDelimiter = boundary + 2s

    let bonusFor prevClass class' =
        let b =
            if class' > CharClass.NonWord then
                match prevClass with
                | CharClass.White -> Some bonusBoundaryWhite         // Word boundary after whitespace
                | CharClass.Delimiter -> Some bonusBoundaryDelimiter // Word boundary after a delimiter character
                | CharClass.NonWord -> Some boundary                 // Word boundary
                | _ -> None
            else
                None

        match b with
        | Some b -> b
        | _ ->
            if prevClass = CharClass.Lower && class' = CharClass.Upper
                || prevClass <> CharClass.Number && class' = CharClass.Number then
                // camelCase letter123
                camel123
            else
                match class' with
                | CharClass.NonWord | CharClass.Delimiter -> nonWord
                | CharClass.White -> bonusBoundaryWhite
                | _ -> 0s

    // A minor optimization that can give yet another 5% performance boost
    let matrix =
        Array2D.init
            (int CharClass.Number + 1)
            (int CharClass.Number + 1)
            (fun i j -> bonusFor (enum<CharClass> i) (enum<CharClass> j))
