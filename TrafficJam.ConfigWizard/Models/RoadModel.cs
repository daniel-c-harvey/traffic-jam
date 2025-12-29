namespace TrafficJam.ConfigWizard.Models;

public enum RoadType { Highway, Arterial, Stroad, Street, Residential }

public class RoadModel : ObservableBase
{
    private string _label = "";
    private double _distance;
    private double _speedLimit;
    private RoadType _roadType = RoadType.Street;
    private int _laneCount = 1;
    private int _sourceId;
    private int _targetId;

    public string Label { get => _label; set => Set(ref _label, value); }
    public double Distance { get => _distance; set => Set(ref _distance, value); }
    public double SpeedLimit { get => _speedLimit; set => Set(ref _speedLimit, value); }
    public RoadType RoadType { get => _roadType; set => Set(ref _roadType, value); }
    public int LaneCount { get => _laneCount; set => Set(ref _laneCount, value); }
    public int SourceId { get => _sourceId; set => Set(ref _sourceId, value); }
    public int TargetId { get => _targetId; set => Set(ref _targetId, value); }

    public string DisplayName => $"{Label}: {SourceId} -> {TargetId}";
}
