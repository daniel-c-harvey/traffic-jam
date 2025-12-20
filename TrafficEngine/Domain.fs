module TrafficEngine.Domain

open TrafficEngine.Units

[<Struct>]
type LaneId = LaneId of int

[<Struct>]
type IntersectionId = IntersectionId of int

[<Struct>]
type RoadId = RoadId of int

[<Struct>]
type LanePosition = {
    Road: RoadId
    Lane: LaneId
}

type IntersectionControl =
    | Uncontrolled
    | YieldSign
    | StopSign of allWay: bool
    | TrafficSignal of phases: int * cycleDuration: float<sec>

type Intersection = {
    Id : IntersectionId
    Label: string
    Control: IntersectionControl
}

type RoadType =
    | Highway
    | Arterial
    | Stroad
    | Street
    | Residential
    
type RoadParameters = {
    Distance: float<m>
    SpeedLimit: float<kmph>
    Lanes: int<lane>
    Capacity: float<vph>
    RoadType: RoadType
}

type Road = {
    Id: RoadId
    Label: string
    Parameters: RoadParameters
}