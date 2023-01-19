using System;
using System.Threading;

namespace ourMIPSSharp_App.ViewModels;

public class Breakpoint {
    public Action<Breakpoint> UpdateAction { get; }
    public int Line { get; set; }
    public bool IsDeleted { get; set; }

    public Breakpoint(Action<Breakpoint> updateAction) {
        UpdateAction = updateAction;
        Update();
    }

    public void Update() => UpdateAction(this);
}