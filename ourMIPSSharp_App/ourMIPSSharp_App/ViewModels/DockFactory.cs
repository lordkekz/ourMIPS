using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.ReactiveUI;
using Dock.Model.ReactiveUI.Controls;
using ourMIPSSharp_App.ViewModels.Editor;
using ourMIPSSharp_App.Views;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class DockFactory : Factory {
    private readonly object _context;
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    private readonly MainViewModel _main;
    private ToolDock _consoleDock;

    public DockFactory(MainViewModel main, object context) {
        _context = context;
        _main = main;
    }

    public override IRootDock CreateLayout() {
        var leftDock = new ProportionalDock {
            Proportion = 0.5,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>
            (
                _documentDock = new DocumentDock {
                    IsCollapsable = false,
                    ActiveDockable = _main.CurrentEditor,
                    VisibleDockables = CreateList<IDockable>(),
                    CanCreateDocument = true
                },
                new ProportionalDockSplitter(),
                _consoleDock = new ToolDock {
                    ActiveDockable = _main.CurrentConsole,
                    VisibleDockables = CreateList<IDockable>(),
                    Alignment = Alignment.Bottom
                }
            )
        };

        _main.FileSwitched += (sender, args) => {
            if (args.DeactivatedFile is null || args.ActivatedFile is null)
                return;

            var c = args.DeactivatedFile.Console;
            if (c.Owner is not IDock dock) return;
            
            var index = 0;
            
            if (dock.VisibleDockables is null)
                dock.VisibleDockables = CreateList<IDockable>();
            else {
                index = dock.VisibleDockables.IndexOf(c);
                dock.VisibleDockables.Remove(c);
            }
            
            if (dock.HiddenDockables is null)
                dock.HiddenDockables = CreateList<IDockable>(c);
            else dock.HiddenDockables.Add(c);

            dock.VisibleDockables.Insert(index, args.ActivatedFile.Console);
        };

        var rightDock = new ProportionalDock {
            Proportion = 0.5,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>
            (
                new ToolDock {
                    ActiveDockable = _main.Registers,
                    VisibleDockables = CreateList<IDockable>(_main.Registers, _main.Memory),
                    Alignment = Alignment.Top,
                    GripMode = GripMode.Hidden
                },
                new ProportionalDockSplitter(),
                new ToolDock {
                    ActiveDockable = _main.Instructions,
                    VisibleDockables = CreateList<IDockable>(_main.Instructions),
                    Alignment = Alignment.Right,
                    GripMode = GripMode.AutoHide
                }
            )
        };

        var mainLayout = new ProportionalDock {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>
            (
                leftDock,
                new ProportionalDockSplitter(),
                rightDock
            )
        };

        var rootDock = CreateRootDock();

        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        _rootDock = rootDock;

        _main.FileOpened += (sender, file) => _documentDock.VisibleDockables?.Add(file.Editor);
        _main.FileSwitched += (sender, args) => _documentDock.ActiveDockable = args.ActivatedFile?.Editor;
        _documentDock.WhenAnyValue(d => d.ActiveDockable).Subscribe(e => {
            if (e is DocumentViewModel m)
                _main.CurrentFile = m.File;
        });

        return rootDock;
    }

    public override void InitLayout(IDockable layout) {
        ContextLocator = new Dictionary<string, Func<object>> {
            ["Document"] = () => new object(),
            ["Console"] = () => new object(),
            ["Instructions"] = () => new object(),
            ["Memory"] = () => new object(),
            ["Registers"] = () => new object(),
            ["MainLayout"] = () => layout
        };

        DockableLocator = new Dictionary<string, Func<IDockable?>>() {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => _documentDock
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow>> {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}