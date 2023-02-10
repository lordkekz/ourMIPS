using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.TextMate.Grammars;

namespace ourMIPSSharp_App.Views; 

public partial class MemoryInitView : UserControl {
    private RegistryOptions _registryOptions;
    private TextMate.Installation _textMateInstallation;
    private readonly TextEditor _textEditor;

    public MemoryInitView() {
        InitializeComponent();
        _textEditor = this.FindControl<TextEditor>("Editor")!;
        _registryOptions = new RegistryOptions(ThemeName.Dark);
        _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
        var jsonLanguage = _registryOptions.GetLanguageByExtension(".json");
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(jsonLanguage.Id));
        
        
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