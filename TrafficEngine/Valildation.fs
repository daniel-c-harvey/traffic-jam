module TrafficEngine.Validation

open TrafficEngine.Domain
open TrafficEngine.Graph
open TrafficEngine.GraphDomain
open TrafficEngine.Units

type ValidationError =
    | MissingJunctionForLane of nodeId: VertexId * lane: Lane
    | UnreachableLane of nodeId: VertexId * lane: Lane
    | InvalidEmitterLane of nodeId: VertexId * lane: Lane
    | InvalidDrainLane of nodeId: VertexId * lane: Lane
    | MissingSignalState of nodeId: VertexId * junction: Junction * phaseIndex: int
    | NotAnIntersection of nodeId: VertexId

let incomingRoads (nodeId: VertexId) (graph: RoadGraph) : Road list =
    graph.Edges
    |> Map.values
    |> Seq.filter (fun edge -> edge.Target = nodeId)
    |> Seq.map (fun edge -> edge.Value)
    |> Seq.toList

let outgoingRoads (nodeId: VertexId) (graph: RoadGraph) : Road list =
    graph.Edges
    |> Map.values
    |> Seq.filter (fun edge -> edge.Source = nodeId)
    |> Seq.map (fun edge -> edge.Value)
    |> Seq.toList
    
let lanesForRoad (road: Road) : Lane list =
    laneRange road.Parameters.LaneCount
    |> List.map (fun n -> { Road = road; Ordinal = LaneNumber n })

let validateIntersection (nodeId: VertexId) (node: Node) (graph: RoadGraph) : ValidationError list =    
    match node with
    | Intersection intersection ->
        let incoming = incomingRoads nodeId graph
        let outgoing = outgoingRoads nodeId graph
        
        let incomingLanes = incoming |> List.collect (fun e -> lanesForRoad e)
        let outgoingLanes = outgoing |> List.collect (fun e -> lanesForRoad e)
        
        let junctionFroms = intersection.Junctions |> List.map (fun j -> j.From) |> Set.ofList
        let junctionTos = intersection.Junctions |> List.map (fun j -> j.To) |> Set.ofList
        
        let missingFroms = 
            incomingLanes 
            |> List.filter (fun lane -> not (Set.contains lane junctionFroms))
            |> List.map (fun lane -> MissingJunctionForLane (nodeId, lane))
        
        let unreachableTos =
            outgoingLanes
            |> List.filter (fun lane -> not (Set.contains lane junctionTos))
            |> List.map (fun lane -> UnreachableLane (nodeId, lane))
        
        missingFroms @ unreachableTos
    | _ -> [ NotAnIntersection (nodeId) ]