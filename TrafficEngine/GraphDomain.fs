module TrafficEngine.GraphDomain

open TrafficEngine.Domain
open TrafficEngine.Units

[<Struct>]
type NodeId = NodeId of int

[<Struct>]
type EdgeId = EdgeId of int

type Node = {
    Id: NodeId
    IntersectionId: IntersectionId
    X: float<m>
    Y: float<m>
}

type Edge = {
    Id: EdgeId
    Source: NodeId
    Target: NodeId
    RoadId: RoadId
}

type RoadGraph = {
    Nodes: Map<NodeId, Node>
    Roads: Map<RoadId, Road>
    NodeIndex: Map<NodeId, int>         // NodeId -> matrix position
    Adjacency: RoadId option[,]
}