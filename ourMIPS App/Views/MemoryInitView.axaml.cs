using System;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace ourMIPSSharp_App.Views;

public partial class MemoryInitView : UserControl {
    private readonly TextEditor _textEditor;

    public MemoryInitView() {
        InitializeComponent();
        
        _textEditor = this.FindControl<TextEditor>("Editor")!;
        
        // Load syntax highlighting definition as resource so it's always available
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>()!;
        using (var bitmap = assets.Open(new Uri("avares://ourMIPSSharp_App/Assets/philosEnv.xshd"))) {
            using (var reader = new XmlTextReader(bitmap)) {
                _textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
        
        _textEditor.AddHandler(PointerWheelChangedEvent, (o, i) => {
            if (i.KeyModifiers != KeyModifiers.Control) return;
            i.Handled = true;
            if (i.Delta.Y > 0) _textEditor.FontSize++;
            else _textEditor.FontSize = _textEditor.FontSize > 1 ? _textEditor.FontSize - 1 : 1;
        }, RoutingStrategies.Tunnel, true);
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}