namespace ourMIPSSharp_App.ViewModels; 

public enum ApplicationState {
    None,
    FileOpened,
    Built,
    Rebuilding,
    Running,
    Debugging
}

public static class ApplicationStateExtensions {
    public static bool IsBuilt(this ApplicationState state) =>
        state is ApplicationState.Built or ApplicationState.Running or ApplicationState.Debugging;
    public static bool IsEmulatorActive(this ApplicationState state) =>
        state is ApplicationState.Running or ApplicationState.Debugging;
    public static bool IsEditingAllowed(this ApplicationState state) =>
        state is not ApplicationState.Debugging;
    public static bool IsRebuildingAllowed(this ApplicationState state) =>
        state is ApplicationState.FileOpened or ApplicationState.Built;
}