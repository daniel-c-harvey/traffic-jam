module TrafficEngine.Configuration

open System.IO
open System.Text.Json
open System.Text.Json.Serialization

let private options =
    let opts = JsonSerializerOptions()
    opts.Converters.Add(JsonFSharpConverter())
    opts.WriteIndented <- true
    opts

let serialize<'T> (value: 'T) : string =
    JsonSerializer.Serialize(value, options)

let deserialize<'T> (json: string) : 'T =
    JsonSerializer.Deserialize<'T>(json, options)

let saveToFile (path: string) (value: 'T) : unit =
    let json = serialize value
    File.WriteAllText(path, json)

let loadFromFile<'T> (path: string) : 'T =
    let json = File.ReadAllText(path)
    deserialize<'T> json