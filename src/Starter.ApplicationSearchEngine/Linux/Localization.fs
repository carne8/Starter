module Starter.ApplicationSearchEngine.Linux.Localization

open System

module Localization =
    type private Locale =
        { Lang: string
          Country: string voption
          Encoding: string voption
          Modifier: string voption }

    let private getRawLocale () =
        let lcAll, lcMessages, lang =
            Environment.GetEnvironmentVariable "LC_ALL" |> ValueOption.ofObj,
            Environment.GetEnvironmentVariable "LC_MESSAGES" |> ValueOption.ofObj,
            Environment.GetEnvironmentVariable "LANG" |> ValueOption.ofObj

        lcAll
        |> ValueOption.orElse lcMessages
        |> ValueOption.orElse lang

    /// lang_COUNTRY.ENCODING@MODIFIER
    type private LocaleParsingState =
        | Lang
        | Country of startIndex: int
        | Encoding of startIndex: int
        | Modifier of startIndex: int

    let private parseLocale (locale: string) =
        let mutable state = Lang
        let mutable lang = ValueNone
        let mutable country = ValueNone
        let mutable encoding = ValueNone
        let mutable modifier = ValueNone

        for i = 0 to locale.Length-1 do
            match state with
            | Lang ->
                match locale[i] with
                | '_' ->
                    lang <- ValueSome locale[..i-1]
                    state <- Country (i+1)
                | '.' ->
                    lang <- ValueSome locale[..i-1]
                    state <- Encoding (i+1)
                | '@' ->
                    lang <- ValueSome locale[..i-1]
                    state <- Modifier (i+1)
                | _ -> ()
            | Country startIndex ->
                match locale[i] with
                | '.' ->
                    country <- ValueSome (locale[startIndex..i-1].ToUpper())
                    state <- Encoding (i+1)
                | '@' ->
                    country <- ValueSome (locale[startIndex..i-1].ToUpper())
                    state <- Modifier (i+1)
                | _ -> ()
            | Encoding startIndex ->
                if locale[i] = '@' then
                    encoding <- ValueSome (locale[startIndex..i-1].ToUpper())
                    state <- Modifier (i+1)
            | _ -> failwithf "Unexpected state: %A" state

        match state with
        | Lang -> lang <- ValueSome locale
        | Country startIndex -> country <- ValueSome (locale[startIndex..].ToUpper())
        | Encoding startIndex -> encoding <- ValueSome (locale[startIndex..].ToUpper())
        | Modifier startIndex -> modifier <- ValueSome (locale[startIndex..].ToUpper())

        match lang with
        | ValueNone -> failwith "No lang detected"
        | ValueSome lang ->
            { Lang = lang
              Country = country
              Encoding = encoding
              Modifier = modifier }

    let getLocaleLookupKeys () =
        let locale =
            getRawLocale ()
            |> ValueOption.map parseLocale

        match locale with
        | ValueNone -> [| |]
        | ValueSome locale ->
            [| // lang_COUNTRY@MODIFIER
               match locale with
               | { Modifier = ValueSome modifier; Country = ValueSome country } -> $"{locale.Lang}_{country}@{modifier}"
               | _ -> ()

               // lang_COUNTRY
               match locale with
               | { Country = ValueSome country } -> $"{locale.Lang}_{country}"
               | _ -> ()

               // lang@MODIFIER
               match locale with
               | { Modifier = ValueSome modifier } -> $"{locale.Lang}@{modifier}"
               | _ -> ()

               // lang
               locale.Lang |]
