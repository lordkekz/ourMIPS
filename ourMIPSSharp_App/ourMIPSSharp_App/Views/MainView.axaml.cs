using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using ourMIPSSharp_App.DebugEditor;
using ourMIPSSharp_App.ViewModels;
using ReactiveUI;

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
        try {
            // Load mult_philos sample from unit tests
            var sourceSample = File.ReadAllText("../../../../../lib_ourMIPSSharp_Tests/Samples/mult_philos.ourMIPS");
            _ = ViewModel!.OpenProgramFromSourceAsync(sourceSample);
        }
        catch (IOException) {
            ViewModel!.Commands.CreateDocumentCommand.Execute();
        }

        // Write ready text to console (immediately applies colors)
        ViewModel.ConsoleWrapper.Clear();
        ViewModel.CurrentFile?.Backend.TextInfoWriter.WriteLine("Ready.");
        _ = ViewModel.ConsoleWrapper.FlushNewLines();


        Interactions.SaveFileTo.RegisterHandler(
            async interaction => {
                var saveOptions = new FilePickerSaveOptions {
                    Title = interaction.Input.Item1,
                    SuggestedFileName = interaction.Input.Item2,
                    DefaultExtension = interaction.Input.Item3,
                    ShowOverwritePrompt = true
                };

                var sp = App.Current?.GetStorageProvider();
                var file = sp is not null ? await sp.SaveFilePickerAsync(saveOptions) : null;

                interaction.SetOutput(file);
            });
        Interactions.OpenProgramFile.RegisterHandler(
            async interaction => {
                var openOptions = new FilePickerOpenOptions {
                    Title = interaction.Input,
                    AllowMultiple = false
                };

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


        if (DockControl.FindDescendantOfType<Button>() is { Name: "PART_ButtonCreate" } newProgramButton) {
            newProgramButton.HotKey = new KeyGesture(Key.N, KeyModifiers.Alt);
            ToolTip.SetTip(newProgramButton, "Create new program (Alt+N)");
        }

        // Register a global command to just close the current file.
        // BUG When closing two tabs after each other while there still are tabs to the right, the command won't be fired. It works again after clicking into the DocumentView or ConsoleView.
        KeyBindings.Add(new KeyBinding {
            Command = ReactiveCommand.Create(() => ViewModel?.CurrentFile?.CloseCommand.Execute()),
            Gesture = new KeyGesture(Key.X, KeyModifiers.Alt)
        });
    }
}