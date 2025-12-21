module TrafficEngine.Graph

[<Struct>]
type VertexId = VertexId of int

[<Struct>]
type EdgeId = EdgeId of int

type Vertex<'Value> = {
    Id: VertexId
    Value: 'Value
}

type Edge<'Value> = {
    Id: EdgeId
    Source: VertexId
    Target: VertexId
    Value: 'Value
}

type Graph<'NodeValue, 'EdgeValue> = {
    Nodes: Map<VertexId, Vertex<'NodeValue>>
    Edges: Map<EdgeId, Edge<'EdgeValue>>
    NodeIndex: Map<VertexId, int>
    Adjacency: EdgeId option[,]
}

type GraphBuilder<'NodeValue, 'EdgeValue> = {
    Nodes: Map<VertexId, Vertex<'NodeValue>>
    Edges: Map<EdgeId, Edge<'EdgeValue>>
}

module GraphBuilder =
    let empty<'NodeValue, 'EdgeValue> : GraphBuilder<'NodeValue, 'EdgeValue> = {
        Nodes = Map.empty
        Edges = Map.empty
    }
    
    let addVertex (vertex: Vertex<'NodeValue>) (builder: GraphBuilder<'NodeValue, 'EdgeValue>) =
        { builder with Nodes = Map.add vertex.Id vertex builder.Nodes }
    
    let addEdge (edge: Edge<'EdgeValue>) (builder: GraphBuilder<'NodeValue, 'EdgeValue>) =
        { builder with Edges = Map.add edge.Id edge builder.Edges }
    
    let build (builder: GraphBuilder<'NodeValue, 'EdgeValue>) : Graph<'NodeValue, 'EdgeValue> =
        let nodeList = builder.Nodes |> Map.keys |> Seq.toArray
        let nodeIndex = nodeList |> Array.mapi (fun i id -> (id, i)) |> Map.ofArray
        let n = Array.length nodeList
        
        let adjacency = Array2D.create n n None
        
        for kvp in builder.Edges do
            let edge = kvp.Value
            match Map.tryFind edge.Source nodeIndex, Map.tryFind edge.Target nodeIndex with
            | Some i, Some j -> adjacency.[i, j] <- Some edge.Id
            | _ -> ()
        
        {
            Nodes = builder.Nodes
            Edges = builder.Edges
            NodeIndex = nodeIndex
            Adjacency = adjacency
        }