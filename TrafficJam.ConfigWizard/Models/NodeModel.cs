namespace TrafficJam.ConfigWizard.Models;

public enum NodeType { Emitter, Drain, Sink, Intersection }
public enum ControlType { Uncontrolled, Yield, StopTwoWay, StopAllWay }

public class NodeModel : ObservableBase
{
    private int _id;
    private string _label = "";
    private NodeType _nodeType = NodeType.Intersection;
    private ControlType _controlType = ControlType.Uncontrolled;
    private double _spawnRate = 100.0;
    private double _x;
    private double _y;

    public int Id { get => _id; set => Set(ref _id, value); }
    public string Label { get => _label; set => Set(ref _label, value); }
    public NodeType NodeType { get => _nodeType; set => Set(ref _nodeType, value); }
    public ControlType ControlType { get => _controlType; set => Set(ref _controlType, value); }
    public double SpawnRate { get => _spawnRate; set => Set(ref _spawnRate, value); }
    public double X { get => _x; set => Set(ref _x, value); }
    public double Y { get => _y; set => Set(ref _y, value); }

    public string DisplayName => $"{Label} ({NodeType})";
}
