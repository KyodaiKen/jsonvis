using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonVis.Models;

public class JsonNodeItem
{
    public string Header { get; set; }
    public List<JsonNodeItem>? Children { get; set; }

    public JsonNodeItem(string propertyName, JsonElement element)
    {
        // Wir bestimmen den Wert-String für das Label
        string displayValue = GetDisplayValue(element);

        // Formatierung des Headers: 
        // Wenn ein propertyName existiert (z.B. "id" oder "[0]"), stellen wir ihn voran.
        if (string.IsNullOrEmpty(propertyName))
        {
            Header = displayValue;
        }
        else
        {
            // Bei Objekten/Arrays zeigen wir nur den Namen, bei Werten den Namen + Wert
            if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
                Header = $"{propertyName} {displayValue}";
            else
                Header = $"{propertyName}: {displayValue}";
        }

        // Rekursion für Kinder
        if (element.ValueKind == JsonValueKind.Object)
        {
            Children = element.EnumerateObject()
                .Select(p => new JsonNodeItem(p.Name, p.Value))
                .ToList();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            Children = element.EnumerateArray()
                .Select((item, index) => new JsonNodeItem($"[{index}]", item))
                .ToList();
        }
    }

    private string GetDisplayValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("id", out var idProp)) return $"(id: {idProp})";
                if (element.TryGetProperty("name", out var nameProp)) return $"(name: {nameProp})";
                return "{ }";

            case JsonValueKind.Array:
                return $"[{element.GetArrayLength()}]";

            case JsonValueKind.String:
                return $"\"{element.GetString()}\"";

            case JsonValueKind.Number:
                return element.GetRawText();

            case JsonValueKind.True: return "true";
            case JsonValueKind.False: return "false";
            case JsonValueKind.Null: return "null";

            default:
                return element.ToString();
        }
    }
}