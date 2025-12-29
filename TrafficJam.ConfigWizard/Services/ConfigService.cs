using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using TrafficJam.ConfigWizard.Models;
using TrafficJam.ConfigWizard.ViewModels;
using FSharpDomain = TrafficEngine.Domain;
using FSharpGraph = TrafficEngine.Graph;
using FSharpConfig = TrafficEngine.Configuration;

namespace TrafficJam.ConfigWizard.Services;

/// <summary>
/// Service for converting between C# view models and F# domain types.
/// F# units of measure are compile-time only and erased at runtime,
/// so we work with raw numeric values here.
/// </summary>
public static class ConfigService
{
    public static FSharpConfig.FileConfig ToFileConfig(NetworkViewModel vm)
    {
        var nodes = vm.Nodes.Select(n => ToFSharpNode(n)).ToList();
        var connections = vm.Roads.Select(r => ToFSharpConnection(r)).ToList();

        var networkConfig = new FSharpConfig.NetworkConfig(
            ListModule.OfSeq(nodes),
            ListModule.OfSeq(connections)
        );

        // TimeStep is float<sec> in F#, but at runtime it's just a double
        return new FSharpConfig.FileConfig(networkConfig, vm.TimeStep);
    }

    public static void FromFileConfig(FSharpConfig.FileConfig config, NetworkViewModel vm)
    {
        foreach (var (vertexId, node) in config.Network.Nodes)
        {
            vm.Nodes.Add(FromFSharpNode(vertexId, node));
        }

        foreach (var (road, sourceId, targetId) in config.Network.Connections)
        {
            vm.Roads.Add(FromFSharpRoad(road, sourceId, targetId));
        }

        vm.TimeStep = config.TimeStep;
    }

    public static void Save(string path, NetworkViewModel vm)
    {
        var config = ToFileConfig(vm);
        FSharpConfig.saveToFile(path, config);
    }

    public static FSharpConfig.FileConfig Load(string path)
    {
        return FSharpConfig.loadFromFile<FSharpConfig.FileConfig>(path);
    }

    private static Tuple<FSharpGraph.VertexId, FSharpDomain.Node> ToFSharpNode(NodeModel model)
    {
        var vertexId = FSharpGraph.VertexId.NewVertexId(model.Id);
        FSharpDomain.Node node = model.NodeType switch
        {
            NodeType.Emitter => FSharpDomain.Node.NewEmitter(new FSharpDomain.Emitter(
                model.Label,
                FSharpList<FSharpDomain.Lane>.Empty,
                model.SpawnRate,  // float<vph> at runtime is just double
                FSharpList<Tuple<FSharpDomain.DriverProfile, double>>.Empty
            )),
            NodeType.Drain => FSharpDomain.Node.NewDrain(new FSharpDomain.Drain(
                model.Label,
                FSharpList<FSharpDomain.Lane>.Empty
            )),
            NodeType.Sink => FSharpDomain.Node.NewSink(new FSharpDomain.Sink(
                model.Label,
                FSharpList<FSharpDomain.Lane>.Empty,
                FSharpList<FSharpDomain.Lane>.Empty,
                model.SpawnRate,
                FSharpList<Tuple<FSharpDomain.DriverProfile, double>>.Empty
            )),
            NodeType.Intersection => FSharpDomain.Node.NewIntersection(new FSharpDomain.Intersection(
                model.Label,
                ToFSharpControl(model.ControlType),
                FSharpList<FSharpDomain.Junction>.Empty
            )),
            _ => throw new ArgumentException($"Unknown node type: {model.NodeType}")
        };
        return Tuple.Create(vertexId, node);
    }

    private static FSharpDomain.IntersectionControl ToFSharpControl(ControlType control) => control switch
    {
        ControlType.Uncontrolled => FSharpDomain.IntersectionControl.Uncontrolled,
        ControlType.Yield => FSharpDomain.IntersectionControl.YieldSign,
        ControlType.StopTwoWay => FSharpDomain.IntersectionControl.NewStopSign(false),
        ControlType.StopAllWay => FSharpDomain.IntersectionControl.NewStopSign(true),
        _ => FSharpDomain.IntersectionControl.Uncontrolled
    };

    private static Tuple<FSharpDomain.Road, FSharpGraph.VertexId, FSharpGraph.VertexId> ToFSharpConnection(RoadModel model)
    {
        // Units (float<m>, float<kmph>, int<lanes>) are erased at runtime
        var roadParams = new FSharpDomain.RoadParameters(
            model.Distance,      // float<m> -> double
            model.SpeedLimit,    // float<kmph> -> double
            ToFSharpRoadType(model.RoadType),
            model.LaneCount      // int<lanes> -> int
        );
        var road = new FSharpDomain.Road(model.Label, roadParams);
        var source = FSharpGraph.VertexId.NewVertexId(model.SourceId);
        var target = FSharpGraph.VertexId.NewVertexId(model.TargetId);
        return Tuple.Create(road, source, target);
    }

    private static FSharpDomain.RoadType ToFSharpRoadType(RoadType roadType) => roadType switch
    {
        RoadType.Highway => FSharpDomain.RoadType.Highway,
        RoadType.Arterial => FSharpDomain.RoadType.Arterial,
        RoadType.Stroad => FSharpDomain.RoadType.Stroad,
        RoadType.Street => FSharpDomain.RoadType.Street,
        RoadType.Residential => FSharpDomain.RoadType.Residential,
        _ => FSharpDomain.RoadType.Street
    };

    private static NodeModel FromFSharpNode(FSharpGraph.VertexId vertexId, FSharpDomain.Node node)
    {
        var model = new NodeModel { Id = vertexId.Item };

        if (node.IsEmitter)
        {
            var e = ((FSharpDomain.Node.Emitter)node).Item;
            model.NodeType = NodeType.Emitter;
            model.Label = e.Label;
            model.SpawnRate = e.SpawnRate;
        }
        else if (node.IsDrain)
        {
            var d = ((FSharpDomain.Node.Drain)node).Item;
            model.NodeType = NodeType.Drain;
            model.Label = d.Label;
        }
        else if (node.IsSink)
        {
            var s = ((FSharpDomain.Node.Sink)node).Item;
            model.NodeType = NodeType.Sink;
            model.Label = s.Label;
            model.SpawnRate = s.SpawnRate;
        }
        else if (node.IsIntersection)
        {
            var i = ((FSharpDomain.Node.Intersection)node).Item;
            model.NodeType = NodeType.Intersection;
            model.Label = i.Label;
            model.ControlType = FromFSharpControl(i.Control);
        }

        return model;
    }

    private static ControlType FromFSharpControl(FSharpDomain.IntersectionControl control)
    {
        if (control.IsUncontrolled) return ControlType.Uncontrolled;
        if (control.IsYieldSign) return ControlType.Yield;
        if (control.IsStopSign)
        {
            var allWay = ((FSharpDomain.IntersectionControl.StopSign)control).allWay;
            return allWay ? ControlType.StopAllWay : ControlType.StopTwoWay;
        }
        return ControlType.Uncontrolled;
    }

    private static RoadModel FromFSharpRoad(FSharpDomain.Road road, FSharpGraph.VertexId source, FSharpGraph.VertexId target)
    {
        return new RoadModel
        {
            Label = road.Label,
            Distance = road.Parameters.Distance,
            SpeedLimit = road.Parameters.SpeedLimit,
            RoadType = FromFSharpRoadType(road.Parameters.RoadType),
            LaneCount = road.Parameters.LaneCount,
            SourceId = source.Item,
            TargetId = target.Item
        };
    }

    private static RoadType FromFSharpRoadType(FSharpDomain.RoadType roadType)
    {
        if (roadType.IsHighway) return RoadType.Highway;
        if (roadType.IsArterial) return RoadType.Arterial;
        if (roadType.IsStroad) return RoadType.Stroad;
        if (roadType.IsStreet) return RoadType.Street;
        if (roadType.IsResidential) return RoadType.Residential;
        return RoadType.Street;
    }
}
