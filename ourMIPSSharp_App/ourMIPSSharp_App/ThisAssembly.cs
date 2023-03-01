// Resharper disable all
internal partial class ThisAssembly {
    public partial class Git {
// Resharper restore all
        public static string SemVerString => SemVer.Major + "." +
                                             SemVer.Minor + "." +
                                             SemVer.Patch + "-" +
                                             Branch + "@" +
                                             Commit;
    }
}