# TrafficEngine - F# Traffic Simulation

## Project Overview

A pure functional traffic simulation engine written in F#. The goal is to learn idiomatic F# through building a digraph-based road network simulation with lane-level granularity, signal timing, driver behavior modeling, and congestion feedback.

**Key Constraint:** Pure functional F# in the backend. C# interop only at the edges (future GUI frontend). No OOP inheritance patterns—use composition, discriminated unions, and module functions.

## Project Structure

```
TrafficEngine/
├── TrafficEngine.Core/
│   ├── TrafficEngine.Core.fsproj
│   ├── Units.fs              # Units of measure (km, m, sec, hr, lanes, etc.)
│   ├── Graph.fs              # Generic graph types and builder
│   ├── Domain.fs             # Traffic domain types (Road, Node, Vehicle, etc.)
│   ├── GraphDomain.fs        # Type aliases wiring Graph to Domain
│   ├── Validation.fs         # Graph validation functions
│   ├── Configuration.fs      # JSON serialization, FileConfig → SimConfig
│   └── Program.fs            # Entry point
├── TrafficEngine.Tests/
│   ├── TrafficEngine.Tests.fsproj
│   ├── DomainTests.fs        # Expecto tests
│   └── Program.fs            # Test runner entry point
```

**F# file order matters!** Files compile top-to-bottom as listed in .fsproj. A file can only reference files above it.

## Core Design Decisions

### Graph Architecture

- **Single graph** with vertices wherever topology changes (intersections, lane merges, lane splits)
- **Vertex (Node)** is a discriminated union: `Emitter | Drain | Intersection`
- **Edge** carries a `Road` value directly (no RoadId indirection—each edge's road is unique)
- **Lane** is a composite key: `{ Road: Road; Ordinal: LaneNumber }` (no separate LaneId)
- **Junction** describes lane-to-lane connectivity through a node: `{ From: Lane; To: Lane }`
- **Adjacency matrix** stores `EdgeId option[,]` for O(1) connectivity lookup

### Configuration vs Runtime

- **FileConfig** — serializable, describes network declaratively
- **SimConfig** — runtime, contains built Graph (derived from FileConfig)
- **Graph is derived**, not serialized (adjacency matrix doesn't serialize well)

### Units of Measure

Used extensively for type safety. Compile-time only, zero runtime cost.

```fsharp
[<Measure>] type m           // meters
[<Measure>] type km          // kilometers  
[<Measure>] type sec         // seconds
[<Measure>] type hr          // hours
[<Measure>] type veh         // vehicles
[<Measure>] type lane        // lane ordinal (ID-like, no arithmetic)
[<Measure>] type lanes       // lane count (quantity)
[<Measure>] type driverScalar // behavioral multipliers
[<Measure>] type kmph = km/hr
[<Measure>] type mps = m/sec
[<Measure>] type vph = veh/hr
```

**Rule:** Use units for physical quantities with arithmetic. Use single-case DUs for identifiers (no arithmetic).

### ID Types (Single-Case DUs)

Prevent mixing up IDs. No arithmetic allowed.

```fsharp
[<Struct>] type NodeId = NodeId of int
[<Struct>] type EdgeId = EdgeId of int
[<Struct>] type VehicleId = VehicleId of int
[<Struct>] type LaneNumber = LaneNumber of int<lane>
```

## Current Type Definitions

### Units.fs

```fsharp
module TrafficEngine.Units

[<Measure>] type km
[<Measure>] type m
[<Measure>] type sec
[<Measure>] type hr
[<Measure>] type veh
[<Measure>] type lane
[<Measure>] type lanes
[<Measure>] type driverScalar

[<Measure>] type kmph = km/hr
[<Measure>] type mps = m/sec
[<Measure>] type vph = veh/hr
[<Measure>] type vpkm = veh/km

// Conversions
let kmToM (d: float<km>) : float<m> = d * 1000.0<m/km>
let mToKm (d: float<m>) : float<km> = d / 1000.0<m/km>
let hrToSec (t: float<hr>) : float<sec> = t * 3600.0<sec/hr>
let secToHr (t: float<sec>) : float<hr> = t / 3600.0<sec/hr>
let kmphToMps (s: float<kmph>) : float<mps> = s * (1000.0<m/km> / 3600.0<sec/hr>)
let mpsToKmph (s: float<mps>) : float<kmph> = s * (3600.0<sec/hr> / 1000.0<m/km>)

let laneRange (count: int<lanes>) : int<lane> list =
    List.init (int count) (fun i -> i * 1<lane>)
```

### Graph.fs

```fsharp
module TrafficEngine.Graph

[<Struct>] type NodeId = NodeId of int
[<Struct>] type EdgeId = EdgeId of int

type Vertex<'Value> = {
    Id: NodeId
    Value: 'Value
}

type Edge<'Value> = {
    Id: EdgeId
    Source: NodeId
    Target: NodeId
    Value: 'Value
}

type Graph<'NodeValue, 'EdgeValue> = {
    Nodes: Map<NodeId, Vertex<'NodeValue>>
    Edges: Map<EdgeId, Edge<'EdgeValue>>
    NodeIndex: Map<NodeId, int>
    Adjacency: EdgeId option[,]
}

type GraphBuilder<'NodeValue, 'EdgeValue> = {
    Nodes: Map<NodeId, Vertex<'NodeValue>>
    Edges: Map<EdgeId, Edge<'EdgeValue>>
}

module GraphBuilder =
    let empty<'NodeValue, 'EdgeValue> : GraphBuilder<'NodeValue, 'EdgeValue>
    let addVertex (vertex: Vertex<'NodeValue>) (builder: GraphBuilder<'NodeValue, 'EdgeValue>)
    let addEdge (edge: Edge<'EdgeValue>) (builder: GraphBuilder<'NodeValue, 'EdgeValue>)
    let build (builder: GraphBuilder<'NodeValue, 'EdgeValue>) : Graph<'NodeValue, 'EdgeValue>
```

### Domain.fs

```fsharp
module TrafficEngine.Domain

open TrafficEngine.Units
open TrafficEngine.Graph

[<Struct>] type LaneNumber = LaneNumber of int<lane>

// Road types
type RoadType = Highway | Arterial | Stroad | Street | Residential

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

// Lane (composite key)
type Lane = {
    Road: Road
    Ordinal: LaneNumber
}

// Junction (lane-to-lane connection)
type Junction = {
    From: Lane
    To: Lane
}

// Signal control
type FlowState = RightOfWay | Yield | Stop

type SignalPhase = {
    Duration: float<sec>
    JunctionStates: Map<Junction, FlowState>
}

type SignalConfig = {
    Phases: SignalPhase list
    YellowDuration: float<sec>
    AllRedDuration: float<sec>
}

type IntersectionControl =
    | Uncontrolled
    | YieldSign
    | StopSign of allWay: bool
    | TrafficSignal of SignalConfig

// Node types (DU)
type Emitter = {
    Label: string
    ToLanes: Lane list
    SpawnRate: float<vph>
    ProfileDistribution: (DriverProfile * float) list
}

type Drain = {
    Label: string
    FromLanes: Lane list
}

type Intersection = {
    Label: string
    Control: IntersectionControl
    Junctions: Junction list
}

type Node =
    | Emitter of Emitter
    | Drain of Drain
    | Intersection of Intersection

// Driver modeling
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

// Vehicle state
[<Struct>] type VehicleId = VehicleId of int

type VehiclePosition = {
    Lane: Lane
    Distance: float<m>
}

type Vehicle = {
    Id: VehicleId
    Profile: DriverProfile
    Position: VehiclePosition
    Speed: float<mps>
    Destination: NodeId
}
```

### GraphDomain.fs

```fsharp
module TrafficEngine.GraphDomain

open TrafficEngine.Graph
open TrafficEngine.Domain

type RoadVertex = Vertex<Node>
type RoadEdge = Edge<Road>
type RoadGraph = Graph<Node, Road>
```

### Configuration.fs

```fsharp
module TrafficEngine.Configuration

open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open TrafficEngine.Graph
open TrafficEngine.Domain

type NetworkConfig = {
    Nodes: (NodeId * Node) list
    Connections: (Road * NodeId * NodeId) list
}

type FileConfig = {
    Network: NetworkConfig
    TimeStep: float<sec>
}

type SimConfig = {
    Graph: Graph<Node, Road>
    TimeStep: float<sec>
}

// JSON options with F# support
let private options = ...

let serialize<'T> (value: 'T) : string
let deserialize<'T> (json: string) : 'T
let saveToFile (path: string) (value: 'T) : unit
let loadFromFile<'T> (path: string) : 'T
let buildGraph (config: NetworkConfig) : Graph<Node, Road>
let buildSimConfig (file: FileConfig) : SimConfig
let loadConfig (path: string) : SimConfig
```

### Validation.fs

```fsharp
module TrafficEngine.Validation

type ValidationError =
    | MissingJunctionForLane of nodeId: NodeId * lane: Lane
    | UnreachableLane of nodeId: NodeId * lane: Lane
    | InvalidEmitterLane of nodeId: NodeId * lane: Lane
    | InvalidDrainLane of nodeId: NodeId * lane: Lane
    | MissingSignalState of nodeId: NodeId * junction: Junction * phaseIndex: int

let lanesForRoad (road: Road) : Lane list
let incomingEdges (nodeId: NodeId) (graph: Graph<'N, Road>) : Edge<Road> list
let outgoingEdges (nodeId: NodeId) (graph: Graph<'N, Road>) : Edge<Road> list
let validateIntersection (nodeId: NodeId) (intersection: Intersection) (graph: Graph<Node, Road>) : ValidationError list
```

## Simulation State (To Implement)

```fsharp
type SignalState = {
    CurrentPhase: int
    TimeInPhase: float<sec>
}

type SimState = {
    Time: float<sec>
    Vehicles: Map<VehicleId, Vehicle>
    Signals: Map<NodeId, SignalState>
}
```

### Step Function (To Implement)

```fsharp
let step (config: SimConfig) (state: SimState) : SimState =
    // 1. Advance signals
    // 2. Compute traffic conditions (congestion)
    // 3. For each vehicle:
    //    a. Decide acceleration based on surroundings
    //    b. Update speed
    //    c. Update position
    //    d. Handle junction transitions
    // 4. Spawn new vehicles at emitters
    // 5. Remove vehicles that reach drains
    { state with Time = state.Time + config.TimeStep }
```

## Remaining Work

### Immediate (Configuration)
1. Test FileConfig serialization round-trip
2. Create example JSON config file
3. Wire up Program.fs to load config and build graph

### Simulation Core
1. Implement `SimState` type
2. Implement signal advancement
3. Implement vehicle spawning at emitters
4. Implement vehicle movement along lanes
5. Implement junction transitions (choosing next lane)
6. Implement vehicle removal at drains
7. Implement congestion calculation

### Routing
1. Implement `chooseJunction` function (destination-based routing)
2. Consider congestion-aware routing

### Validation
1. Complete `validateEmitter` and `validateDrain`
2. Implement `validateSignalConfig` (exhaustive junction states per phase)
3. Wire validation into config loading

### Outputs (Mentioned in Requirements)
- Road capacity utilization
- Deviant flow (traffic not at designed parameters)
- Turbulence at nodes and along edges

## F# Idioms Reference

### Pattern Matching on DUs
```fsharp
match node with
| Emitter e -> e.ToLanes
| Drain d -> d.FromLanes
| Intersection i -> i.Junctions
```

### Pipe Operator
```fsharp
items
|> List.filter predicate
|> List.map transform
|> List.fold folder initial
```

### Record Update (Copy-On-Write)
```fsharp
let updated = { original with Field = newValue }
```

### Option Handling
```fsharp
match Map.tryFind key map with
| Some value -> // use value
| None -> // handle missing
```

### Result for Validation
```fsharp
type Result<'T, 'E> = Ok of 'T | Error of 'E
```

## Test Framework

Using **Expecto**. Tests are values, run as executable.

```fsharp
[<Tests>]
let myTests = testList "GroupName" [
    test "test name" {
        Expect.equal actual expected "message"
    }
]
```

Run: `dotnet run --project TrafficEngine.Tests`

## Dependencies

- **FSharp.SystemTextJson** — JSON serialization with F# type support
- **Expecto** — Test framework

## User Context

Daniel is an experienced C#/.NET developer (20 years programming, 10 in C#) learning F# through this project. Emphasize pure functional patterns, avoid OOP thinking. He understands monads, set theory, category theory concepts. Keep explanations connected to the math when helpful.
