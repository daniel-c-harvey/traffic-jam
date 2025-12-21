module TrafficEngine.Simulation

open TrafficEngine.Domain
open TrafficEngine.GraphDomain
open TrafficEngine.Units

type NetworkConfig = {
    Nodes: (NodeId * Node) list
    Connections: (Road * NodeId * NodeId) list
}

type FileConfig = {
    Network: NetworkConfig
    TimeStep: float<sec>
}

type SimConfig = {
    Graph: RoadGraph
    TimeStep: float<sec>
}