using System;
using System.Collections.ObjectModel;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class MainViewModel : ViewModelBase {
    public OpenScriptBackend Backend { get; private set; }
    public ObservableCollection<InstructionEntry> InstructionList { get; } = new();

    public MainViewModel() {
        // Load mult_philos sample from unit tests
        Backend = new OpenScriptBackend("../../../../../lib_ourMIPSSharp_Tests/Samples/mult_philos.ourMIPS");
        UpdateData();
    }

    public void UpdateData() {
        if (!Backend.Ready)
            return;
        
        InstructionList.Clear();
        var prog = Backend.CurrentEmulator!.Program;
        for (var i = 0; i < prog.Count; i++) {
            InstructionList.Add(new InstructionEntry(i, prog));
        }
    }
}