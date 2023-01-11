using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.EmulatorComponents;

public class RegisterStorage {
    public Emulator? Owner { get; }
    public short ProgramCounter { get; set; }
    public bool FlagOverflow { get; set; }
    private readonly int[] _registers;

    public RegisterStorage(Emulator? owner = null) {
        Owner = owner;
        _registers = new int[32];

        var r = new Random();
        for (var i = 1; i < 32; i++)
            _registers[i] = r.Next(int.MinValue, int.MaxValue);

        // Init stackpointer to max signed int
        _registers[(int)Register.Sp] = int.MaxValue;

        // Global pointer is random in philos; no special init needed
    }

    public int this[Register reg] {
        get => _registers[(int)reg];
        set {
            if (reg == Register.Zero) {
                if (Owner is null)
                    Console.WriteLine("Warning! Writes to zero register have no effect!");
                else
                    Owner?.TextInfo.WriteLine("Warning! Writes to zero register have no effect!");
            }
            else
                _registers[(int)reg] = value;
        }
    }
}