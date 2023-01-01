namespace lib_ourMIPSSharp.Emulator;

public class RegisterStorage {
    public short ProgramCounter { get; set; }
    public bool FlagOverflow { get; set; }
    private readonly int[] _registers;

    public RegisterStorage() {
        _registers = new int[32];

        var r = new Random();
        for (var i = 1; i < 32; i++)
            _registers[i] = r.Next();
    }

    public int this[Register reg] {
        get => _registers[(int)reg];
        set {
            if (reg == Register.Zero)
                Console.WriteLine("Warning! Writes to zero register have no effect!");
            else
                _registers[(int)reg] = value;
        }
    }
}