module TrafficEngine.Tests.DomainTests

open TrafficEngine
open TrafficEngine.Units
open TrafficEngine.Domain
open TrafficEngine.Graph
open Expecto
open TrafficEngine.Validation

[<Tests>]
let laneTests = testList "Lane" [
    test "lane is keyed by road and ordinal" {
        let road = {
            Label = "Main St"
            Parameters = {
                Distance = 100.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 2<lanes>
            }
        }
        let lane = { Road = road; Ordinal = LaneNumber 0<lane> }
        
        Expect.equal lane.Road road "road should match"
        Expect.equal lane.Ordinal (LaneNumber 0<lane>) "ordinal should match"
    }
]

[<Tests>]
let junctionTests = testList "Junction" [
    test "junction connects two lanes" {
        let roadA = {
            Label = "Road A"
            Parameters = {
                Distance = 100.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 1<lanes>
            }
        }
        let roadB = {
            Label = "Road B"
            Parameters = {
                Distance = 100.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 1<lanes>
            }
        }
        
        let from = { Road = roadA; Ordinal = LaneNumber 0<lane> }
        let to' = { Road = roadB; Ordinal = LaneNumber 0<lane> }
        
        let junction = { From = from; To = to' }
        
        Expect.equal junction.From.Road roadA "from road"
        Expect.equal junction.To.Road roadB "to road"
    }
    
    test "lane merge has multiple junctions to same target" {
        let roadA = {
            Label = "Road A"
            Parameters = {
                Distance = 100.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 2<lanes>
            }
        }
        let roadB = {
            Label = "Road B"
            Parameters = {
                Distance = 100.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 1<lanes>
            }
        }
        
        let mergeJunctions = [
            { From = { Road = roadA; Ordinal = LaneNumber 0<lane> }
              To = { Road = roadB; Ordinal = LaneNumber 0<lane> } }
            { From = { Road = roadA; Ordinal = LaneNumber 1<lane> }
              To = { Road = roadB; Ordinal = LaneNumber 0<lane> } }
        ]
        
        Expect.equal (List.length mergeJunctions) 2 "two lanes merge into one"
    }
]

[<Tests>]
let nodeTests = testList "Node" [
    test "emitter has only outgoing lanes" {
        let road = {
            Label = "Highway"
            Parameters = {
                Distance = 1000.0<m>
                SpeedLimit = 100.0<kmph>
                RoadType = Highway
                LaneCount = 2<lanes>
            }
        }
        
        let emitter = Emitter {
            Label = "Highway On-Ramp"
            ToLanes = [
                { Road = road; Ordinal = LaneNumber 0<lane> }
                { Road = road; Ordinal = LaneNumber 1<lane> }
            ]
        }
        
        match emitter with
        | Emitter e -> Expect.equal (List.length e.ToLanes) 2 "two outgoing lanes"
        | _ -> failtest "expected Emitter"
    }
    
    test "drain has only incoming lanes" {
        let road = {
            Label = "Highway"
            Parameters = {
                Distance = 1000.0<m>
                SpeedLimit = 100.0<kmph>
                RoadType = Highway
                LaneCount = 1<lanes>
            }
        }
        
        let drain = Drain {
            Label = "Highway Off-Ramp"
            FromLanes = [
                { Road = road; Ordinal = LaneNumber 0<lane> }
            ]
        }
        
        match drain with
        | Drain d -> Expect.equal (List.length d.FromLanes) 1 "one incoming lane"
        | _ -> failtest "expected Drain"
    }
    
    test "intersection has complete junction routing" {
        let roadN = {
            Label = "North Road"
            Parameters = {
                Distance = 100.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 1<lanes>
            }
        }
        let roadS = {
            Label = "South Road"
            Parameters = {
                Distance = 100.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 1<lanes>
            }
        }
        
        let intersection = Intersection {
            Label = "Main & 1st"
            Control = Uncontrolled
            Junctions = [
                { From = { Road = roadN; Ordinal = LaneNumber 0<lane> }
                  To = { Road = roadS; Ordinal = LaneNumber 0<lane> } }
            ]
        }
        
        match intersection with
        | Intersection i -> 
            Expect.equal i.Label "Main & 1st" "label"
            Expect.equal (List.length i.Junctions) 1 "one junction"
        | _ -> failtest "expected Intersection"
    }
]

[<Tests>]
let graphTests = testList "Graph" [
    test "simple linear network with lane merge" {
        let roadA = {
            Label = "Road A"
            Parameters = {
                Distance = 500.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 2<lanes>
            }
        }
        
        let roadB = {
            Label = "Road B"
            Parameters = {
                Distance = 300.0<m>
                SpeedLimit = 40.0<kmph>
                RoadType = Residential
                LaneCount = 1<lanes>
            }
        }
        
        let roadALane0 = { Road = roadA; Ordinal = LaneNumber 0<lane> }
        let roadALane1 = { Road = roadA; Ordinal = LaneNumber 1<lane> }
        let roadBLane0 = { Road = roadB; Ordinal = LaneNumber 0<lane> }
        
        let emitter = Emitter {
            Label = "Entry Point"
            ToLanes = [ roadALane0; roadALane1 ]
        }
        
        let intersection = Intersection {
            Label = "Merge Point"
            Control = Uncontrolled
            Junctions = [
                { From = roadALane0; To = roadBLane0 }
                { From = roadALane1; To = roadBLane0 }
            ]
        }
        
        let drain = Drain {
            Label = "Exit Point"
            FromLanes = [ roadBLane0 ]
        }
        
        match emitter with
        | Emitter e -> Expect.equal (List.length e.ToLanes) 2 "emitter has 2 outgoing lanes"
        | _ -> failtest "expected Emitter"
        
        match intersection with
        | Intersection i -> Expect.equal (List.length i.Junctions) 2 "intersection has 2 junctions for merge"
        | _ -> failtest "expected Intersection"
        
        match drain with
        | Drain d -> Expect.equal (List.length d.FromLanes) 1 "drain has 1 incoming lane"
        | _ -> failtest "expected Drain"
    }
]

[<Tests>]
let graphBuildTests = testList "GraphBuilder" [
    test "build linear network graph" {
        let roadA = {
            Label = "Road A"
            Parameters = {
                Distance = 500.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 2<lanes>
            }
        }
        
        let roadB = {
            Label = "Road B"
            Parameters = {
                Distance = 300.0<m>
                SpeedLimit = 40.0<kmph>
                RoadType = Residential
                LaneCount = 1<lanes>
            }
        }
        
        let roadALane0 = { Road = roadA; Ordinal = LaneNumber 0<lane> }
        let roadALane1 = { Road = roadA; Ordinal = LaneNumber 1<lane> }
        let roadBLane0 = { Road = roadB; Ordinal = LaneNumber 0<lane> }
        
        let emitter = Emitter {
            Label = "Entry"
            ToLanes = [ roadALane0; roadALane1 ]
        }
        
        let intersection = Intersection {
            Label = "Merge"
            Control = Uncontrolled
            Junctions = [
                { From = roadALane0; To = roadBLane0 }
                { From = roadALane1; To = roadBLane0 }
            ]
        }
        
        let drain = Drain {
            Label = "Exit"
            FromLanes = [ roadBLane0 ]
        }
        
        let roadAEdgeId = EdgeId 0
        let roadBEdgeId = EdgeId 1
        
        let graph =
            GraphBuilder.empty
            |> GraphBuilder.addVertex { Id = NodeId 0; Value = emitter }
            |> GraphBuilder.addVertex { Id = NodeId 1; Value = intersection }
            |> GraphBuilder.addVertex { Id = NodeId 2; Value = drain }
            |> GraphBuilder.addEdge { Id = roadAEdgeId; Source = NodeId 0; Target = NodeId 1; Value = roadA }
            |> GraphBuilder.addEdge { Id = roadBEdgeId; Source = NodeId 1; Target = NodeId 2; Value = roadB }
            |> GraphBuilder.build
        
        Expect.equal (Map.count graph.Nodes) 3 "three nodes"
        Expect.equal (Map.count graph.Edges) 2 "two edges"
        
        let idx0 = Map.find (NodeId 0) graph.NodeIndex
        let idx1 = Map.find (NodeId 1) graph.NodeIndex
        let idx2 = Map.find (NodeId 2) graph.NodeIndex
        
        Expect.equal graph.Adjacency.[idx0, idx1] (Some roadAEdgeId) "road A connects emitter to intersection"
        Expect.equal graph.Adjacency.[idx1, idx2] (Some roadBEdgeId) "road B connects intersection to drain"
        Expect.equal graph.Adjacency.[idx0, idx2] None "no direct path from emitter to drain"
    }
]

[<Tests>]
let validationTests = testList "Validation" [
    test "incomplete intersection fails validation" {
        let roadA = {
            Label = "Road A"
            Parameters = {
                Distance = 500.0<m>
                SpeedLimit = 50.0<kmph>
                RoadType = Street
                LaneCount = 2<lanes>
            }
        }
        
        let roadB = {
            Label = "Road B"
            Parameters = {
                Distance = 300.0<m>
                SpeedLimit = 40.0<kmph>
                RoadType = Residential
                LaneCount = 1<lanes>
            }
        }
        
        let roadALane0 = { Road = roadA; Ordinal = LaneNumber 0<lane> }
        let roadALane1 = { Road = roadA; Ordinal = LaneNumber 1<lane> }
        let roadBLane0 = { Road = roadB; Ordinal = LaneNumber 0<lane> }
        
        // Only route lane 0, forget lane 1
        let intersection = Intersection {
            Label = "Incomplete Merge"
            Control = Uncontrolled
            Junctions = [
                { From = roadALane0; To = roadBLane0 }
                // Missing: { From = roadALane1; To = roadBLane0 }
            ]
        }
        
        let graph =
            GraphBuilder.empty
            |> GraphBuilder.addVertex { Id = NodeId 0; Value = Emitter { Label = "Entry"; ToLanes = [ roadALane0; roadALane1 ] } }
            |> GraphBuilder.addVertex { Id = NodeId 1; Value = intersection }
            |> GraphBuilder.addVertex { Id = NodeId 2; Value = Drain { Label = "Exit"; FromLanes = [ roadBLane0 ] } }
            |> GraphBuilder.addEdge { Id = EdgeId 0; Source = NodeId 0; Target = NodeId 1; Value = roadA }
            |> GraphBuilder.addEdge { Id = EdgeId 1; Source = NodeId 1; Target = NodeId 2; Value = roadB }
            |> GraphBuilder.build
        
        let errors = validateIntersection (NodeId 1) intersection graph
                     
        Expect.isNonEmpty errors "should have validation errors"
        Expect.equal (List.length errors) 1 "one missing junction"
    }
]