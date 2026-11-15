module Starter.Features.PlatformInterop.IPC

open System.IO
open System.IO.Pipes
open System.Threading.Tasks

let [<Literal>] private pipeName = "starter-pipe"

type Listener() =
    let pipe = new NamedPipeServerStream(pipeName, PipeDirection.In)

    let receivedEvent = Event<string>()

    do
        Task.Run<unit>(fun () -> task {
            try
                while true do
                    do! pipe.WaitForConnectionAsync()

                    use reader = new StreamReader(pipe, leaveOpen = true)
                    let! line = reader.ReadLineAsync()

                    match line with
                    | null -> ()
                    | line -> receivedEvent.Trigger line

                    if pipe.IsConnected then pipe.Disconnect()
            with e ->
                Serilog.Log.Error(e, "IPC listener thrown an error.")
        }) |> ignore

    [<CLIEvent>]
    member this.ReceivedString = receivedEvent.Publish

let sendString (s: string) =
    use pipe = new NamedPipeClientStream(pipeName)
    pipe.Connect(5000)

    use writer = new StreamWriter(pipe)
    writer.WriteLine s
    writer.Flush()
    writer.Close()
