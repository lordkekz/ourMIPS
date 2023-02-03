using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DialogHostAvalonia;
using ourMIPSSharp_App.DebugEditor;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Views;

using Pair = KeyValuePair<int, Control>;

public partial class MainView : UserControl {
    public MainViewModel? ViewModel => DataContext as MainViewModel;

    private readonly BreakPointMargin _breakPointMargin;
    private readonly EditorDebugCurrentLineHighlighter _lineHightlighter;

    public MainView() {
        InitializeComponent();
    }

    protected override void OnLoaded() {
        base.OnLoaded();

        this.TryFindColor("SystemBaseMediumColor", out var infoColor);
        this.TryFindColor("SystemBaseHighColor", out var normalColor);
        ConView.LineBrushes = new List<IBrush>() {
            new SolidColorBrush(infoColor),
            new SolidColorBrush(normalColor),
            Brushes.OrangeRed,
            Brushes.LimeGreen
        };
        
        // Load mult_philos sample from unit tests
        var sourceSample = File.ReadAllText("../../../../../lib_ourMIPSSharp_Tests/Samples/mult_philos.ourMIPS");
        ViewModel!.OpenProgramFromSource(sourceSample);
        
        // Write ready text to console (immediately applies colors)
        ViewModel.CurrentConsole!.Clear();
        ViewModel.CurrentBackend!.TextInfoWriter.WriteLine("Ready.");
        _ = ViewModel.CurrentConsole.FlushNewLines();

        var saveOptions = new FilePickerSaveOptions {
            Title = "Save program...",
            DefaultExtension = "ourMIPS",
            ShowOverwritePrompt = true,
            SuggestedFileName = "program"
        };
        var openOptions = new FilePickerOpenOptions {
            Title = "Open program...",
            AllowMultiple = false
        };
        MainViewModel.SaveFileTo.RegisterHandler(
            async interaction => {
                var sp = App.Current?.GetStorageProvider();
                var file = sp is not null ? await sp.SaveFilePickerAsync(saveOptions) : null;

                interaction.SetOutput(file);
            });
        MainViewModel.OpenProgramFile.RegisterHandler(
            async interaction => {
                var sp = App.Current?.GetStorageProvider();
                var file = sp is not null ? await sp.OpenFilePickerAsync(openOptions) : null;

                interaction.SetOutput(file?.FirstOrDefault());
            });
        MainViewModel.AskSaveChanges.RegisterHandler(
            async interaction => {
                var result = await DialogHost.Show(new ModalDialogViewModel {
                    Text = "Save Changes?"
                });

                interaction.SetOutput((bool)(result ?? false));
            });
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (sender is DataGrid grid && e.AddedItems.Count > 0)
            grid.SelectedItems.Clear();
    }
}