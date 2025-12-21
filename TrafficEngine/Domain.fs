module TrafficEngine.Domain

open TrafficEngine.Units
open TrafficEngine.Graph

[<Struct>]
type LaneNumber = LaneNumber of int<lane>

[<Struct>]
type IntersectionId = IntersectionId of int

type DriverParameters = {
    ReactionTime: float<driverScalar>
    Aggression: float<driverScalar>
    Courtesy: float<driverScalar>
}

type DriverRoutine =
    | Commuter of workStart: float<hr> * workEnd: float<hr> * lunchBreak: bool
    | ServiceWorker of shiftStart: float<hr> * shiftDuration: float<hr>
    | Cruising

type DriverProfile = {
    Parameters: DriverParameters
    Routine: DriverRoutine
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
    RoadType: RoadType
    LaneCount: int<lanes>
}

type Road = {
    Label: string
    Parameters: RoadParameters
}

type Lane = {
    Road: Road
    Ordinal: LaneNumber
}

type Emitter = {
    Label: string
    ToLanes: Lane list    // lanes that originate here
    SpawnRate: float<vph>
    ProfileDistribution: (DriverProfile * float) list
}

type Drain = {
    Label: string
    FromLanes: Lane list  // lanes that terminate here
}

type Sink = {
    Label: string
    ToLanes: Lane list    // lanes that originate here (emitter behavior)
    FromLanes: Lane list  // lanes that terminate here (drain behavior)
    SpawnRate: float<vph>
    ProfileDistribution: (DriverProfile * float) list
}

type Junction = {
    From: Lane
    To: Lane
}

type FlowState =
    | RightOfWay
    | Yield
    | Stop

type SignalPhase = {
    Duration: float<sec>
    JunctionStates: Map<Junction, FlowState>
}

type SignalConfig = {
    Phases: SignalPhase list
    YellowDuration: float<sec>
    AllRedDuration: float<sec>    // clearance interval between phases
}

type IntersectionControl =
    | Uncontrolled
    | YieldSign
    | StopSign of allWay: bool
    | TrafficSignal of SignalConfig

type Intersection = {
    Label: string
    Control: IntersectionControl
    Junctions: Junction list
}

type Node =
    | Emitter of Emitter
    | Drain of Drain
    | Sink of Sink
    | Intersection of Intersection

[<Struct>]
type VehicleId = VehicleId of int

type VehiclePosition = {
    Lane: Lane
    Distance: float<m>      // how far along the lane
}

type Vehicle = {
    Id: VehicleId
    Profile: DriverProfile
    Position: VehiclePosition
    Speed: float<mps>
    Destination: Node
}