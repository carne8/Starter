module Starter.WebSearchEngine.Logger

open Serilog

let mutable logger: ILogger = null
let setLogger l = logger <- l
