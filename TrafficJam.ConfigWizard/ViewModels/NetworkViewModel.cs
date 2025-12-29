using System.Collections.ObjectModel;
using System.Windows.Input;
using TrafficJam.ConfigWizard.Models;
using TrafficJam.ConfigWizard.Services;

namespace TrafficJam.ConfigWizard.ViewModels;

public class NetworkViewModel : ObservableBase
{
    private NodeModel? _selectedNode;
    private RoadModel? _selectedRoad;
    private double _timeStep = 0.1;
    private int _nextNodeId;

    public ObservableCollection<NodeModel> Nodes { get; } = [];
    public ObservableCollection<RoadModel> Roads { get; } = [];

    public NodeModel? SelectedNode { get => _selectedNode; set => Set(ref _selectedNode, value); }
    public RoadModel? SelectedRoad { get => _selectedRoad; set => Set(ref _selectedRoad, value); }
    public double TimeStep { get => _timeStep; set => Set(ref _timeStep, value); }

    public ICommand AddNodeCommand { get; }
    public ICommand AddRoadCommand { get; }
    public ICommand DeleteNodeCommand { get; }
    public ICommand DeleteRoadCommand { get; }

    public NetworkViewModel()
    {
        AddNodeCommand = new Command<NodeType>(AddNode);
        AddRoadCommand = new Command(AddRoad, () => Nodes.Count >= 2);
        DeleteNodeCommand = new Command(DeleteNode, () => SelectedNode != null);
        DeleteRoadCommand = new Command(DeleteRoad, () => SelectedRoad != null);
    }

    private void AddNode(NodeType nodeType)
    {
        var node = new NodeModel
        {
            Id = _nextNodeId++,
            Label = $"Node {_nextNodeId}",
            NodeType = nodeType,
            X = 100 + (Nodes.Count % 5) * 120,
            Y = 100 + (Nodes.Count / 5) * 100
        };
        Nodes.Add(node);
        SelectedNode = node;
    }

    private void AddRoad()
    {
        if (Nodes.Count < 2) return;
        var road = new RoadModel
        {
            Label = $"Road {Roads.Count + 1}",
            Distance = 500,
            SpeedLimit = 50,
            LaneCount = 2,
            SourceId = Nodes[0].Id,
            TargetId = Nodes[1].Id
        };
        Roads.Add(road);
        SelectedRoad = road;
    }

    private void DeleteNode()
    {
        if (SelectedNode == null) return;
        var id = SelectedNode.Id;
        Nodes.Remove(SelectedNode);
        var toRemove = Roads.Where(r => r.SourceId == id || r.TargetId == id).ToList();
        foreach (var road in toRemove) Roads.Remove(road);
        SelectedNode = null;
    }

    private void DeleteRoad()
    {
        if (SelectedRoad == null) return;
        Roads.Remove(SelectedRoad);
        SelectedRoad = null;
    }

    public void Clear()
    {
        Nodes.Clear();
        Roads.Clear();
        _nextNodeId = 0;
        SelectedNode = null;
        SelectedRoad = null;
    }

    public void LoadFrom(TrafficEngine.Configuration.FileConfig config)
    {
        Clear();
        ConfigService.FromFileConfig(config, this);
        _nextNodeId = Nodes.Count > 0 ? Nodes.Max(n => n.Id) + 1 : 0;
    }
}
