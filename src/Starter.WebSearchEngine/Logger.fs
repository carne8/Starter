module Starter.WebSearchEngine.Logger

open Serilog.Core

let mutable logger: Logger = null
let setLogger l = logger <- l
