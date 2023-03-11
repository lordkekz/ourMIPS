#region

using System;

#endregion

namespace ourMIPS_App.Models;

public class Breakpoint {
    
    /// <summary>
    /// An Action performed on the instance to update its values.
    /// </summary>
    public Action<Breakpoint> UpdateAction { get; }
    
    /// <summary>
    /// The Line number of this Breakpoint.
    /// </summary>
    public int Line { get; set; }
    
    /// <summary>
    /// Whether this Breakpoint was deleted by the user.
    /// </summary>
    public bool IsDeleted { get; set; }

    public Breakpoint(Action<Breakpoint> updateAction) {
        UpdateAction = updateAction;
        Update();
    }

    /// <summary>
    /// Runs UpdateAction.
    /// </summary>
    public void Update() => UpdateAction(this);
}