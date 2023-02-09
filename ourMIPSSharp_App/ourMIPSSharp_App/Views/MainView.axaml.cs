using System;
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

        // Load mult_philos sample from unit tests
        var sourceSample = File.ReadAllText("../../../../../lib_ourMIPSSharp_Tests/Samples/mult_philos.ourMIPS");
        _ = ViewModel!.OpenProgramFromSourceAsync(sourceSample);

        // Write ready text to console (immediately applies colors)
        ViewModel.ConsoleWrapper.Clear();
        ViewModel.CurrentFile?.Backend.TextInfoWriter.WriteLine("Ready.");
        _ = ViewModel.ConsoleWrapper.FlushNewLines();

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
        Interactions.SaveFileTo.RegisterHandler(
            async interaction => {
                var sp = App.Current?.GetStorageProvider();
                var file = sp is not null ? await sp.SaveFilePickerAsync(saveOptions) : null;

                interaction.SetOutput(file);
            });
        Interactions.OpenProgramFile.RegisterHandler(
            async interaction => {
                var sp = App.Current?.GetStorageProvider();
                var file = sp is not null ? await sp.OpenFilePickerAsync(openOptions) : null;

                interaction.SetOutput(file?.FirstOrDefault());
            });
        Interactions.AskSaveChanges.RegisterHandler(
            async interaction => {
                var result = await DialogHost.Show(new ModalDialogViewModel {
                    Text = "Save Changes?"
                });

                interaction.SetOutput((bool)(result ?? false));
            });
    }
}