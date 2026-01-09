using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Primitives; // Wichtig für ScrollBarVisibility
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using JsonVis.ViewModels;
using JsonVis.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Data.Converters;

namespace JsonVis.Views;

public partial class MainWindow : Window
{
    private readonly Panel _container;
    private readonly TreeView _treeView;
    private readonly ScrollViewer _treeScrollViewer;
    private readonly StackPanel _welcomeArea;
    private List<JsonNodeItem> _searchResults = new();
    private int _currentSearchIndex = -1;
    private string _lastQuery = string.Empty;
    private TextBlock _lblStatus;

    public MainWindow()
    {
        Title = "JSONVis - Tree Viewer";
        Width = 800;
        Height = 600;
        Background = Brush.Parse("#1e1e1e");

        var rootPanel = new DockPanel();

        // 1. Menü erstellen
        var menu = new Menu { Background = Brush.Parse("#2d2d2d") };
        DockPanel.SetDock(menu, Dock.Top);
        var fileMenu = new MenuItem { Header = "_File", Foreground = Brushes.White };
        var openItem = new MenuItem { Header = "_Open" };
        openItem.Click += OpenFile_Click;
        fileMenu.Items.Add(openItem);
        menu.Items.Add(fileMenu);

        // 2. Suchleiste erstellen
        var searchGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
            Background = Brush.Parse("#2d2d2d")
        };
        DockPanel.SetDock(searchGrid, Dock.Top);

        var btnPrev = new Button { Content = "▲", Width = 35, Background = Brushes.Transparent, Foreground = Brushes.White };
        btnPrev.Click += (s, e) => NavigateSearch(-1);

        var btnNext = new Button { Content = "▼", Width = 35, Background = Brushes.Transparent, Foreground = Brushes.White };
        btnNext.Click += (s, e) => NavigateSearch(1);

        var searchBox = new TextBox
        {
            Watermark = "Search...",
            Background = Brush.Parse("#333333"),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
        };

        // Im Konstruktor:
        _lblStatus = new TextBlock
        {
            Text = "0/0",
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            FontSize = 12
        };

        var searchContainer = new Panel();
        searchContainer.Children.Add(searchBox);
        var statusWrapper = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { _lblStatus }
        };
        searchContainer.Children.Add(statusWrapper);

        searchBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) NavigateSearch(1); };
        searchBox.GetObservable(TextBox.TextProperty).Subscribe(text => UpdateSearch(text, _lblStatus));

        searchGrid.Children.Add(btnPrev); Grid.SetColumn(btnPrev, 0);
        searchGrid.Children.Add(btnNext); Grid.SetColumn(btnNext, 1);
        searchGrid.Children.Add(searchContainer); Grid.SetColumn(searchContainer, 2);

        // 3. Hauptbereich (TreeView & ScrollViewer)
        _container = new Panel();
        // 1. Den TreeView erstellen
        _treeView = new TreeView
        {
            Foreground = Brushes.White,
            IsVisible = false,
            Background = Brushes.Transparent,
            FontFamily = new FontFamily("JetBrains Mono, DejaVu Sans Mono, monospace"),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 2. DAS TEMPLATE (Das behebt die Anzeige "JsonVis.Models...")
        _treeView.ItemTemplate = new FuncTreeDataTemplate<JsonNodeItem>(
            (item, _) =>
            {
                var tb = new TextBlock
                {
                    // Hier wird definiert, was wirklich im Text stehen soll:
                    Text = item.Header,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Das Highlight-Binding für die Suche
                tb.Bind(TextBlock.BackgroundProperty, new Binding("IsMatch")
                {
                    Converter = new FuncValueConverter<bool, IBrush>(isMatch =>
                        isMatch ? Brush.Parse("#664400") : Brushes.Transparent)
                });

                return tb;
            },
            // Hier wird definiert, wo die Kinder für die Baumstruktur herkommen:
            item => item.Children ?? Enumerable.Empty<JsonNodeItem>()
        );

        var treeViewItemStyle = new Style(x => x.OfType<TreeViewItem>());

        // Setzt das Padding auf 0 (Top/Bottom) und 5 (Left/Right für Text-Abstand zum Pfeil)
        treeViewItemStyle.Setters.Add(new Setter(TreeViewItem.PaddingProperty, new Thickness(5, 0)));

        // WICHTIG: Die Mindesthöhe erzwingt oft den Abstand, wir setzen sie auf 0
        treeViewItemStyle.Setters.Add(new Setter(TreeViewItem.MinHeightProperty, 0.0));

        // Verknüpfung für das Aufklappen bei Suche (IsExpanded)
        treeViewItemStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty,
            new Binding("IsExpanded") { Mode = BindingMode.TwoWay }));

        // Verhindert das "Stretchen" für korrekte horizontale Scrollbalken
        treeViewItemStyle.Setters.Add(new Setter(TreeViewItem.HorizontalAlignmentProperty, HorizontalAlignment.Left));

        _treeView.Styles.Add(treeViewItemStyle);

        // --- ScrollViewer Konfiguration ---
        _treeScrollViewer = new ScrollViewer
        {
            Content = _treeView,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, // Jetzt wird er bei Bedarf sichtbar
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsVisible = false
        };

        _welcomeArea = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 20 };
        var welcomeText = new TextBlock { Text = "No JSON file loaded", HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Gray };
        var openButton = new Button { Content = "Browse file", HorizontalAlignment = HorizontalAlignment.Center };
        openButton.Click += OpenFile_Click;
        _welcomeArea.Children.Add(welcomeText);
        _welcomeArea.Children.Add(openButton);

        _container.Children.Add(_treeScrollViewer);
        _container.Children.Add(_welcomeArea);

        // --- WICHTIG: Jedes Element nur EINMAL hinzufügen ---
        rootPanel.Children.Add(menu);       // 1. Oben
        rootPanel.Children.Add(searchGrid); // 2. Darunter
        rootPanel.Children.Add(_container); // 3. Füllt den Rest

        Content = rootPanel;
        _treeView.Bind(ItemsControl.ItemsSourceProperty, new Binding("Items"));
    }

    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open JSON File",
            FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            await vm.LoadJsonFile(files[0].Path.LocalPath);

            // UI Umschalten: Welcome weg, ScrollViewer (mit TreeView) her
            _welcomeArea.IsVisible = false;
            _treeView.IsVisible = true; // Sicherstellen, dass der Baum selbst aktiv ist
            _treeScrollViewer.IsVisible = true; // Den Container einblenden!
        }
    }

    public async void LoadFileFromArgs(string filePath)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.LoadJsonFile(filePath);
            _welcomeArea.IsVisible = false;
            _treeView.IsVisible = true;
            _treeScrollViewer.IsVisible = true;
        }
    }

    private void SearchInTree(string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || DataContext is not MainWindowViewModel vm)
            return;

        string lowQuery = query.ToLower();

        // Wenn es eine neue Suche ist, Liste neu aufbauen
        if (lowQuery != _lastQuery)
        {
            _searchResults.Clear();
            foreach (var item in vm.Items)
            {
                FindAllMatches(item, lowQuery, _searchResults);
            }
            _currentSearchIndex = -1;
            _lastQuery = lowQuery;
        }

        if (_searchResults.Count == 0) return;

        // Index erhöhen und ggf. vorne anfangen (Loop)
        _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
        var targetNode = _searchResults[_currentSearchIndex];

        // Den Pfad zum Ziel-Knoten öffnen
        foreach (var rootItem in vm.Items)
        {
            ExpandPathTo(rootItem, targetNode);
        }

        // Selektieren und Scrollen
        _treeView.SelectedItem = targetNode;

        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            await System.Threading.Tasks.Task.Delay(50);
            var container = _treeView.ContainerFromItem(targetNode);
            container?.BringIntoView();
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    // Findet alle Knoten, die den Text enthalten
    private void FindAllMatches(JsonNodeItem node, string query, List<JsonNodeItem> results)
    {
        if (node.Header.ToLower().Contains(query))
        {
            results.Add(node);
        }

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                FindAllMatches(child, query, results);
            }
        }
    }

    // Hilfsmethode: Klappt gezielt die Eltern eines gefundenen Knotens auf
    private bool ExpandPathTo(JsonNodeItem root, JsonNodeItem target)
    {
        if (root == target) return true;

        if (root.Children != null)
        {
            foreach (var child in root.Children)
            {
                if (ExpandPathTo(child, target))
                {
                    root.IsExpanded = true;
                    return true;
                }
            }
        }
        return false;
    }
    // Hilfsmethode, um das exakte Objekt zu finden, das den Text enthält
    private JsonNodeItem? FindSpecificNode(JsonNodeItem node, string query)
    {
        if (node.Header.ToLower().Contains(query)) return node;
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                var found = FindSpecificNode(child, query);
                if (found != null) return found;
            }
        }
        return null;
    }

    private void UpdateSearch(string? query, TextBlock statusLabel)
    {
        // 1. ViewModel sicher abrufen
        var vm = DataContext as MainWindowViewModel;

        // 2. Prüfen, ob die Suche leer ist ODER das ViewModel fehlt
        if (string.IsNullOrWhiteSpace(query) || vm == null)
        {
            _searchResults.Clear();
            _currentSearchIndex = -1;
            statusLabel.Text = "0/0";

            // Highlights nur zurücksetzen, wenn vm nicht null ist
            if (vm != null)
            {
                foreach (var item in vm.Items) ResetHighlights(item);
            }
            return;
        }

        // 3. Ab hier ist 'vm' garantiert zugewiesen und nicht null
        _lastQuery = query.ToLower();
        _searchResults.Clear();

        foreach (var item in vm.Items)
        {
            ResetHighlights(item);
            FindAllMatches(item, _lastQuery, _searchResults);
        }

        _currentSearchIndex = _searchResults.Count > 0 ? 0 : -1;
        statusLabel.Text = $"{(_currentSearchIndex + 1)}/{_searchResults.Count}";

        if (_searchResults.Count > 0) JumpToResult();
    }

    private void NavigateSearch(int direction)
    {
        if (_searchResults.Count == 0) return;

        _currentSearchIndex += direction;

        // Loop-Logik
        if (_currentSearchIndex >= _searchResults.Count) _currentSearchIndex = 0;
        if (_currentSearchIndex < 0) _currentSearchIndex = _searchResults.Count - 1;

        // Wir müssen das Label finden, um den Text zu aktualisieren
        // Da wir es im Konstruktor lokal erstellt haben, ist es am einfachsten, 
        // JumpToResult() aufzurufen und dort das Label mit zu übergeben oder global zu machen.
        JumpToResult();
    }

    private void JumpToResult()
    {
        if (_currentSearchIndex < 0 || _currentSearchIndex >= _searchResults.Count) return;

        var targetNode = _searchResults[_currentSearchIndex];

        if (DataContext is MainWindowViewModel vm)
        {
            foreach (var rootItem in vm.Items)
                ExpandPathTo(rootItem, targetNode);
        }

        _treeView.SelectedItem = targetNode;

        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            await System.Threading.Tasks.Task.Delay(50);
            var container = _treeView.ContainerFromItem(targetNode);
            container?.BringIntoView();
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    // Hilfsmethode zum Zurücksetzen der Highlights (optional, falls du IsMatch nutzt)
    private void ResetHighlights(JsonNodeItem node)
    {
        node.IsMatch = false;
        if (node.Children != null)
        {
            foreach (var child in node.Children) ResetHighlights(child);
        }
    }
}