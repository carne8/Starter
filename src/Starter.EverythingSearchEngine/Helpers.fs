module Starter.EverythingSearchEngine.Helpers

type EarlyReturnBuilder() =
    member this.Bind(m, f) = if m then f()
    member this.While(guard, body) =
        if guard() then
            this.Run(body)
            this.While(guard, body)
        else
            this.Zero()

    member this.Zero() = ()
    member this.Delay(f) = f
    member this.Run(f) = f()

    member this.Combine(f1, f2) =
        f1
        this.Run(f2)

let earlyReturn = EarlyReturnBuilder()
