module TrafficEngine.Program

open TrafficEngine.Configuration

[<EntryPoint>]
let main args =
    match args with
    | [| configPath |] ->
        let config = loadConfig configPath
        printfn "Loaded graph with %d nodes" (Map.count config.Graph.Nodes)
        0
    | _ ->
        printfn "Usage: TrafficEngine <config.json>"
        1
