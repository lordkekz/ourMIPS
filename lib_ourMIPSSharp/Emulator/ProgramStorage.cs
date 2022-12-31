namespace lib_ourMIPSSharp.Emulator;

public class ProgramStorage : List<Instruction> {
    private Dictionary<int, string> _stringConstants = new();

    public ProgramStorage(IEnumerable<uint> instructions, string stringConstants) {
        foreach (var u in instructions)
            Add(new Instruction(u));

        var address = 0;
        foreach (var s in stringConstants.Split('\0')) {
            _stringConstants[address] = s;
            address += 1 + s.Length;
        }
    }

    public string GetStringConstant(int index) => _stringConstants[index];
    public bool ContainsStringConstant(int index) => _stringConstants.ContainsKey(index);
}