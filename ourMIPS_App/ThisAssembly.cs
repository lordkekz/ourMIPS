// Resharper disable all
internal partial class ThisAssembly {
    public partial class Git {
// Resharper restore all
        public static string SemVerString => global::ThisAssembly.Git.SemVer.Major + "." +
                                             global::ThisAssembly.Git.SemVer.Minor + "." +
                                             global::ThisAssembly.Git.SemVer.Patch + "-" +
                                             global::ThisAssembly.Git.Branch + "@" +
                                             global::ThisAssembly.Git.Commit;
    }
}