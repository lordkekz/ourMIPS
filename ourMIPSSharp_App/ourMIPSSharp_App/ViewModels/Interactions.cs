using System.Reactive;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels; 

public static class Interactions {
    public static Interaction<Unit, IStorageFile?> SaveFileTo { get; } = new();
    public static Interaction<Unit, IStorageFile?> OpenProgramFile { get; } = new();
    public static Interaction<Unit, bool> AskSaveChanges { get; } = new();
}