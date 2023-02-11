using System;
using System.Reactive;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels; 

public static class Interactions {
    public static Interaction<(string, string, string), IStorageFile?> SaveFileTo { get; } = new();
    public static Interaction<string, IStorageFile?> OpenProgramFile { get; } = new();
    public static Interaction<Unit, bool> AskSaveChanges { get; } = new();
}