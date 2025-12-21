module TrafficEngine.Units

// =============================================================================
// Base Measures
// =============================================================================

[<Measure>] type km
[<Measure>] type m
[<Measure>] type sec
[<Measure>] type hr
[<Measure>] type veh
[<Measure>] type lane
[<Measure>] type lanes
[<Measure>] type driverScalar

// =============================================================================
// Derived Measures
// =============================================================================

[<Measure>] type kmph = km/hr
[<Measure>] type mps = m/sec
[<Measure>] type vph = veh/hr           // flow: vehicles per hour
[<Measure>] type vpkm = veh/km          // density: vehicles per km

// =============================================================================
// Conversions
// =============================================================================

let kmToM (d: float<km>) : float<m> = d * 1000.0<m/km>
let mToKm (d: float<m>) : float<km> = d / 1000.0<m/km>

let hrToSec (t: float<hr>) : float<sec> = t * 3600.0<sec/hr>
let secToHr (t: float<sec>) : float<hr> = t / 3600.0<sec/hr>

let kmphToMps (s: float<kmph>) : float<mps> = s * (1000.0<m/km> / 3600.0<sec/hr>)
let mpsToKmph (s: float<mps>) : float<kmph> = s * (3600.0<sec/hr> / 1000.0<m/km>)

let laneRange (count: int<lanes>) : int<lane> list =
    List.init (int count) (fun i -> i * 1<lane>)