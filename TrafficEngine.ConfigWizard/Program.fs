module TrafficEngine.ConfigWizard.Program

open System
open TrafficEngine.Units
open TrafficEngine.Graph
open TrafficEngine.Domain
open TrafficEngine.Configuration

// Prompt helpers
let prompt (msg: string) =
    printf "%s: " msg
    Console.ReadLine()

let promptFloat msg =
    prompt msg |> float

let promptInt msg =
    prompt msg |> int

let promptChoice msg (options: string list) =
    printfn "%s" msg
    options |> List.iteri (fun i opt -> printfn "  %d. %s" (i + 1) opt)
    printf "Choice: "
    (Console.ReadLine() |> int) - 1

// State
let mutable nodes: (VertexId * Node) list = []
let mutable connections: (Road * VertexId * VertexId) list = []
let mutable timeStep = 0.1<sec>
let mutable nextVertexId = 0

let getNextVertexId () =
    let id = VertexId nextVertexId
    nextVertexId <- nextVertexId + 1
    id

// Node builders
let buildEmitter () =
    let label = prompt "Emitter label"
    let rate = promptFloat "Spawn rate (vehicles/hour)"
    Emitter {
        Label = label
        ToLanes = []  // Will be populated when roads are added
        SpawnRate = rate * 1.0<vph>
        ProfileDistribution = []
    }

let buildDrain () =
    let label = prompt "Drain label"
    Drain {
        Label = label
        FromLanes = []  // Will be populated when roads are added
    }

let buildSink () =
    let label = prompt "Sink label"
    let rate = promptFloat "Spawn rate (vehicles/hour)"
    Sink {
        Label = label
        ToLanes = []
        FromLanes = []
        SpawnRate = rate * 1.0<vph>
        ProfileDistribution = []
    }

let buildIntersection () =
    let label = prompt "Intersection label"
    let controlChoice = promptChoice "Control type" ["Uncontrolled"; "Yield"; "Stop (2-way)"; "Stop (all-way)"]
    let control =
        match controlChoice with
        | 0 -> Uncontrolled
        | 1 -> YieldSign
        | 2 -> StopSign false
        | _ -> StopSign true
    Intersection {
        Label = label
        Control = control
        Junctions = []
    }

let addNode () =
    let nodeType = promptChoice "Node type" ["Emitter"; "Drain"; "Sink (bidirectional terminus)"; "Intersection"]
    let node =
        match nodeType with
        | 0 -> buildEmitter ()
        | 1 -> buildDrain ()
        | 2 -> buildSink ()
        | _ -> buildIntersection ()
    let id = getNextVertexId ()
    nodes <- nodes @ [(id, node)]
    printfn "Added node %d" (match id with VertexId i -> i)

// Road builder
let addRoad () =
    if List.length nodes < 2 then
        printfn "Need at least 2 nodes to add a road"
    else
        printfn "Existing nodes:"
        nodes |> List.iter (fun (VertexId i, node) ->
            let label = match node with
                        | Emitter e -> e.Label
                        | Drain d -> d.Label
                        | Sink s -> s.Label
                        | Intersection x -> x.Label
            printfn "  %d. %s" i label)

        let source = VertexId (promptInt "Source node ID")
        let target = VertexId (promptInt "Target node ID")
        let label = prompt "Road label"
        let distance = promptFloat "Distance (meters)"
        let speed = promptFloat "Speed limit (km/h)"
        let roadTypeChoice = promptChoice "Road type" ["Highway"; "Arterial"; "Stroad"; "Street"; "Residential"]
        let roadType =
            match roadTypeChoice with
            | 0 -> Highway
            | 1 -> Arterial
            | 2 -> Stroad
            | 3 -> Street
            | _ -> Residential
        let laneCount = promptInt "Lane count"

        let bidirectional = promptChoice "Direction" ["One-way"; "Bidirectional"] = 1

        let road = {
            Label = label
            Parameters = {
                Distance = distance * 1.0<m>
                SpeedLimit = speed * 1.0<kmph>
                RoadType = roadType
                LaneCount = laneCount * 1<lanes>
            }
        }
        connections <- connections @ [(road, source, target)]
        if bidirectional then
            let reverseRoad = { road with Label = label + " (reverse)" }
            connections <- connections @ [(reverseRoad, target, source)]
            printfn "Added bidirectional road: %s" label
        else
            printfn "Added road: %s" label

let showSummary () =
    printfn "\n=== Network Summary ==="
    printfn "Nodes (%d):" (List.length nodes)
    nodes |> List.iter (fun (VertexId i, node) ->
        let (nodeType, label) = match node with
                                | Emitter e -> ("Emitter", e.Label)
                                | Drain d -> ("Drain", d.Label)
                                | Sink s -> ("Sink", s.Label)
                                | Intersection x -> ("Intersection", x.Label)
        printfn "  [%d] %s: %s" i nodeType label)
    printfn "Roads (%d):" (List.length connections)
    connections |> List.iter (fun (road, VertexId s, VertexId t) ->
        printfn "  %s: %d -> %d (%d lanes)" road.Label s t (int road.Parameters.LaneCount))
    printfn "TimeStep: %.2f sec" (float timeStep)
    printfn ""

let saveConfig () =
    let path = prompt "Save path (e.g., config.json)"
    let config: FileConfig = {
        Network = {
            Nodes = nodes
            Connections = connections
        }
        TimeStep = timeStep
    }
    saveToFile path config
    printfn "Saved to %s" path

let loadConfig () =
    let path = prompt "Load path"
    try
        let config = loadFromFile<FileConfig> path
        nodes <- config.Network.Nodes
        connections <- config.Network.Connections
        timeStep <- config.TimeStep
        nextVertexId <- nodes |> List.map (fun (VertexId i, _) -> i) |> List.max |> (+) 1
        printfn "Loaded from %s" path
    with ex ->
        printfn "Error: %s" ex.Message

let setTimeStep () =
    let ts = promptFloat "Time step (seconds)"
    timeStep <- ts * 1.0<sec>

let deleteNode () =
    if List.isEmpty nodes then
        printfn "No nodes to delete"
    else
        showSummary ()
        let id = VertexId (promptInt "Node ID to delete")
        let before = List.length nodes
        nodes <- nodes |> List.filter (fun (vid, _) -> vid <> id)
        connections <- connections |> List.filter (fun (_, s, t) -> s <> id && t <> id)
        if List.length nodes < before then
            printfn "Deleted node %d and its connected roads" (match id with VertexId i -> i)
        else
            printfn "Node not found"

let deleteRoad () =
    if List.isEmpty connections then
        printfn "No roads to delete"
    else
        printfn "Roads:"
        connections |> List.iteri (fun i (road, VertexId s, VertexId t) ->
            printfn "  %d. %s: %d -> %d" i road.Label s t)
        let idx = promptInt "Road index to delete"
        if idx >= 0 && idx < List.length connections then
            let (road, _, _) = connections.[idx]
            connections <- connections |> List.indexed |> List.filter (fun (i, _) -> i <> idx) |> List.map snd
            printfn "Deleted road: %s" road.Label
        else
            printfn "Invalid index"

[<EntryPoint>]
let main _ =
    printfn "=== Traffic Network Config Wizard ==="

    let mutable running = true
    while running do
        let choice = promptChoice "\nMain Menu" [
            "Add node"
            "Add road"
            "Delete node"
            "Delete road"
            "Set timestep"
            "Show summary"
            "Save config"
            "Load config"
            "Exit"
        ]
        match choice with
        | 0 -> addNode ()
        | 1 -> addRoad ()
        | 2 -> deleteNode ()
        | 3 -> deleteRoad ()
        | 4 -> setTimeStep ()
        | 5 -> showSummary ()
        | 6 -> saveConfig ()
        | 7 -> loadConfig ()
        | _ -> running <- false

    0
