using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
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
                    ActiveDockable = _main.CurrentFile,
                    VisibleDockables = CreateList<IDockable>(),
                    CanCreateDocument = true,
                    CanClose = false,
                    CanFloat = false,
                    CanPin = false
                },
                new ProportionalDockSplitter(),
                _consoleDock = new ToolDock {
                    ActiveDockable = _main.ConsoleWrapper,
                    VisibleDockables = CreateList<IDockable>(_main.ConsoleWrapper, _main.Problems),
                    Alignment = Alignment.Top,
                    GripMode = GripMode.Visible,
                    CanClose = false,
                    CanFloat = false,
                    CanPin = false
                }
            ),
            CanClose = false,
            CanFloat = false,
            CanPin = false
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
                    GripMode = GripMode.Visible,
                    CanClose = false,
                    CanFloat = false,
                    CanPin = false
                },
                new ProportionalDockSplitter(),
                new ToolDock {
                    ActiveDockable = _main.Instructions,
                    VisibleDockables = CreateList<IDockable>(_main.Instructions),
                    Alignment = Alignment.Top,
                    GripMode = GripMode.Visible,
                    CanClose = false,
                    CanFloat = false,
                    CanPin = false
                }
            ),
            CanClose = false,
            CanFloat = false,
            CanPin = false
        };

        var mainLayout = new ProportionalDock {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>
            (
                leftDock,
                new ProportionalDockSplitter(),
                rightDock
            ),
            CanClose = false,
            CanFloat = false,
            CanPin = false
        };

        var rootDock = CreateRootDock();

        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        _rootDock = rootDock;

        _main.FileOpened += (sender, file) => {
            _documentDock.VisibleDockables?.Add(file);
            _documentDock.ActiveDockable = file;
            file.Factory = this;
        };
        _main.WhenAnyValue(m => m.CurrentFile).Subscribe(f => {
            if (f?.Owner is DocumentDock d)
                d.ActiveDockable = f;
        });
        _documentDock.WhenAnyValue(d => d.ActiveDockable).Subscribe(e => {
            if (e is DocumentViewModel m)
                _main.CurrentFile = m;
        });

        _documentDock.CreateDocument = _main.Commands.CreateDocumentCommand;

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

    public void DebugEvents() {
        ActiveDockableChanged += (_, args) => {
            Debug.WriteLine($"[ActiveDockableChanged] Title='{args.Dockable?.Title}'");
        };

        FocusedDockableChanged += (_, args) => {
            Debug.WriteLine($"[FocusedDockableChanged] Title='{args.Dockable?.Title}'");
        };

        DockableAdded += (_, args) => { Debug.WriteLine($"[DockableAdded] Title='{args.Dockable?.Title}'"); };

        DockableRemoved += (_, args) => { Debug.WriteLine($"[DockableRemoved] Title='{args.Dockable?.Title}'"); };

        DockableClosed += (_, args) => { Debug.WriteLine($"[DockableClosed] Title='{args.Dockable?.Title}'"); };

        DockableMoved += (_, args) => { Debug.WriteLine($"[DockableMoved] Title='{args.Dockable?.Title}'"); };

        DockableSwapped += (_, args) => { Debug.WriteLine($"[DockableSwapped] Title='{args.Dockable?.Title}'"); };

        DockablePinned += (_, args) => { Debug.WriteLine($"[DockablePinned] Title='{args.Dockable?.Title}'"); };

        DockableUnpinned += (_, args) => { Debug.WriteLine($"[DockableUnpinned] Title='{args.Dockable?.Title}'"); };

        WindowOpened += (_, args) => { Debug.WriteLine($"[WindowOpened] Title='{args.Window?.Title}'"); };

        WindowClosed += (_, args) => { Debug.WriteLine($"[WindowClosed] Title='{args.Window?.Title}'"); };

        WindowClosing += (_, args) => {
            // NOTE: Set to True to cancel window closing.
#if false
                args.Cancel = true;
#endif
            Debug.WriteLine($"[WindowClosing] Title='{args.Window?.Title}', Cancel={args.Cancel}");
        };

        WindowAdded += (_, args) => { Debug.WriteLine($"[WindowAdded] Title='{args.Window?.Title}'"); };

        WindowRemoved += (_, args) => { Debug.WriteLine($"[WindowRemoved] Title='{args.Window?.Title}'"); };

        WindowMoveDragBegin += (_, args) => {
            // NOTE: Set to True to cancel window dragging.
#if false
                args.Cancel = true;
#endif
            Debug.WriteLine(
                $"[WindowMoveDragBegin] Title='{args.Window?.Title}', Cancel={args.Cancel}, X='{args.Window?.X}', Y='{args.Window?.Y}'");
        };

        WindowMoveDrag += (_, args) => {
            Debug.WriteLine(
                $"[WindowMoveDrag] Title='{args.Window?.Title}', X='{args.Window?.X}', Y='{args.Window?.Y}");
        };

        WindowMoveDragEnd += (_, args) => {
            Debug.WriteLine(
                $"[WindowMoveDragEnd] Title='{args.Window?.Title}', X='{args.Window?.X}', Y='{args.Window?.Y}");
        };
    }
}