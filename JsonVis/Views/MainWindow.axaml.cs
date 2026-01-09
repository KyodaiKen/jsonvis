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

namespace JsonVis.Views;

public partial class MainWindow : Window
{
    private readonly Panel _container;
    private readonly TreeView _treeView;
    private readonly ScrollViewer _treeScrollViewer;
    private readonly StackPanel _welcomeArea;

    public MainWindow()
    {
        Title = "JSONVis - Tree Viewer";
        Width = 800;
        Height = 600;
        Background = Brush.Parse("#1e1e1e");

        var rootPanel = new DockPanel();

        // 1. Menü
        var menu = new Menu { Background = Brush.Parse("#2d2d2d") };
        DockPanel.SetDock(menu, Dock.Top);
        var fileMenu = new MenuItem { Header = "_File", Foreground = Brushes.White };
        var openItem = new MenuItem { Header = "_Open" };
        openItem.Click += OpenFile_Click;
        fileMenu.Items.Add(openItem);
        menu.Items.Add(fileMenu);

        // 2. Hauptbereich (Container)
        _container = new Panel();

        // --- ZUERST: TreeView erstellen ---
        _treeView = new TreeView
        {
            Foreground = Brushes.White,
            IsVisible = false,
            Background = Brushes.Transparent,
            FontFamily = new FontFamily("JetBrains Mono, DejaVu Sans Mono, monospace"),
            Margin = new Thickness(0)
        };

        _treeView.ItemTemplate = new FuncTreeDataTemplate<JsonNodeItem>(
            (item, _) => new TextBlock
            {
                Text = item.Header,
                Foreground = Brushes.White,
                FontSize = 13,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Margin = new Thickness(0)
            },
            item => item.Children ?? Enumerable.Empty<JsonNodeItem>()
        );

        // 2. Den Style für die Items anpassen
        var treeViewItemStyle = new Style(x => x.OfType<TreeViewItem>());
        treeViewItemStyle.Setters.Add(new Setter(TreeViewItem.PaddingProperty, new Thickness(2, 0, 0, 0)));
        treeViewItemStyle.Setters.Add(new Setter(TreeViewItem.MinHeightProperty, 18.0));

        // Dies sorgt dafür, dass die Zeile (der Container) die volle Breite nutzt
        treeViewItemStyle.Setters.Add(new Setter(TreeViewItem.HorizontalAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Stretch));

        _treeView.Styles.Add(treeViewItemStyle);

        _treeView.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        _treeView.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        _treeView.AddHandler(Control.RequestBringIntoViewEvent, (s, e) => e.Handled = true, RoutingStrategies.Bubble);

        // --- ZUERST: ScrollViewer erstellen ---
        _treeScrollViewer = new ScrollViewer
        {
            Content = _treeView,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsVisible = false
        };

        // --- ZUERST: Willkommens-Bereich erstellen ---
        _welcomeArea = new StackPanel
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 20
        };

        var welcomeText = new TextBlock
        {
            Text = "No JSON file loaded",
            FontSize = 20,
            Foreground = Brushes.Gray
        };

        var openButton = new Button
        {
            Content = "Browse file",
            Padding = new Thickness(15, 10),
            Background = Brushes.RoyalBlue,
            Foreground = Brushes.White
        };
        openButton.Click += OpenFile_Click;

        _welcomeArea.Children.Add(welcomeText);
        _welcomeArea.Children.Add(openButton);

        // JETZT, wo alles initialisiert ist, zum Container hinzufügen
        // (Einmalig und in der richtigen Reihenfolge)
        _container.Children.Add(_treeScrollViewer);
        _container.Children.Add(_welcomeArea);

        rootPanel.Children.Add(menu);
        rootPanel.Children.Add(_container);
        Content = rootPanel;

        // Data-Binding für den TreeView (Wichtig, damit Daten angezeigt werden)
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
}