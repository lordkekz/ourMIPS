internal partial class ThisAssembly {
    public partial class Git {
        public static string SemVerString => SemVer.Major + "." +
                                             SemVer.Minor + "." +
                                             SemVer.Patch + "-" +
                                             Branch + "+" +
                                             Commit;
    }
}