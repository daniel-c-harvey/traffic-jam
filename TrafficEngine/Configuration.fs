module TrafficEngine.Configuration

open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open TrafficEngine.Graph
open TrafficEngine.Domain
open TrafficEngine.GraphDomain
open TrafficEngine.Units

let private options =
    let opts = JsonSerializerOptions()
    opts.Converters.Add(JsonFSharpConverter())
    opts.WriteIndented <- true
    opts

let serialize<'T> (value: 'T) : string =
    JsonSerializer.Serialize(value, options)

let deserialize<'T> (json: string) : 'T =
    JsonSerializer.Deserialize<'T>(json, options)

let saveToFile (path: string) (value: 'T) : unit =
    let json = serialize value
    File.WriteAllText(path, json)

let loadFromFile<'T> (path: string) : 'T =
    let json = File.ReadAllText(path)
    deserialize<'T> json

// Configuration types

type NetworkConfig = {
    Nodes: (VertexId * Node) list
    Connections: (Road * VertexId * VertexId) list
}

type FileConfig = {
    Network: NetworkConfig
    TimeStep: float<sec>
}

type SimConfig = {
    Graph: RoadGraph
    TimeStep: float<sec>
}

// Graph building

let buildGraph (config: NetworkConfig) : RoadGraph =
    let builder =
        config.Nodes
        |> List.fold (fun b (id, node) ->
            GraphBuilder.addVertex { Id = id; Value = node } b
        ) GraphBuilder.empty

    let builder =
        config.Connections
        |> List.fold (fun (b, nextId) (road, source, target) ->
            let edge = {
                Id = EdgeId nextId
                Source = source
                Target = target
                Value = road
            }
            (GraphBuilder.addEdge edge b, nextId + 1)
        ) (builder, 0)
        |> fst

    GraphBuilder.build builder

let buildSimConfig (file: FileConfig) : SimConfig =
    {
        Graph = buildGraph file.Network
        TimeStep = file.TimeStep
    }

let loadConfig (path: string) : SimConfig =
    let fileConfig = loadFromFile<FileConfig> path
    buildSimConfig fileConfig