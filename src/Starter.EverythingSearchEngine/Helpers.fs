module Starter.EverythingSearchEngine.Helpers

type EarlyReturnBuilder() =
    member this.Bind(m, f) = if m then f()
    member this.While(guard, body) =
        if guard () then
            body()
            this.While(guard, body)
        else
            this.Zero()
    member this.Zero() = ()
    member this.Delay(f) = f
    member this.Run(f) = f()

let earlyReturn = EarlyReturnBuilder()
