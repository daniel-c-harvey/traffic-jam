module TrafficEngine.GraphDomain

open TrafficEngine.Graph
open TrafficEngine.Domain

type RoadVertex = Vertex<Node>
type RoadEdge = Edge<Road>
type RoadGraph = Graph<Node, Road>
