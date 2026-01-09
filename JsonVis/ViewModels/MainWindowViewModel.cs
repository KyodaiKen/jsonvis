using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using JsonVis.Models;
using ReactiveUI;

namespace JsonVis.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<JsonNodeItem> _items = new();
    public ObservableCollection<JsonNodeItem> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    public async Task LoadJsonFile(string filePath)
    {
        try
        {
            string jsonString = await File.ReadAllTextAsync(filePath);
            using var doc = JsonDocument.Parse(jsonString);

            var root = new JsonNodeItem("Root", doc.RootElement.Clone());
            Items = new ObservableCollection<JsonNodeItem> { root };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Laden: {ex.Message}");
        }
    }
}