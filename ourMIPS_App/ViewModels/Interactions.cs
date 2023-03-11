#region

using System.Reactive;
using Avalonia.Platform.Storage;
using ReactiveUI;

#endregion

namespace ourMIPS_App.ViewModels; 

public static class Interactions {
    public static Interaction<(string, string, string), IStorageFile?> SaveFileTo { get; } = new();
    public static Interaction<string, IStorageFile?> OpenProgramFile { get; } = new();
    public static Interaction<Unit, bool> AskSaveChanges { get; } = new();
}