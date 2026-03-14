using System.Text.Json.Serialization;

namespace TaskInspector.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Status
{
    Open,
    InProgress,
    Completed
}