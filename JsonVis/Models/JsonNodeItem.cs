using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ReactiveUI;

namespace JsonVis.Models;

public class JsonNodeItem : ReactiveObject
{
    public string Header { get; set; }
    public List<JsonNodeItem>? Children { get; set; }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    private bool _isMatch;
    public bool IsMatch
    {
        get => _isMatch;
        set => this.RaiseAndSetIfChanged(ref _isMatch, value);
    }

    public JsonNodeItem(string propertyName, JsonElement element)
    {
        string displayValue = GetDisplayValue(element);
        if (string.IsNullOrEmpty(propertyName))
            Header = displayValue;
        else
            Header = (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
                ? $"{propertyName} {displayValue}" : $"{propertyName}: {displayValue}";

        if (element.ValueKind == JsonValueKind.Object)
            Children = element.EnumerateObject().Select(p => new JsonNodeItem(p.Name, p.Value)).ToList();
        else if (element.ValueKind == JsonValueKind.Array)
            Children = element.EnumerateArray().Select((item, index) => new JsonNodeItem($"[{index}]", item)).ToList();
    }

    private string GetDisplayValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.TryGetProperty("id", out var id) ? $"(id: {id})" : (element.TryGetProperty("name", out var name) ? $"(name: {name})" : "{ }"),
            JsonValueKind.Array => $"[{element.GetArrayLength()}]",
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.ToString(),
        };
    }
}