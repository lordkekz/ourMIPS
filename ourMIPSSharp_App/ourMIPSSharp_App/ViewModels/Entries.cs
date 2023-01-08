using System;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;

namespace ourMIPSSharp_App.ViewModels;

public class InstructionEntry {
    private int _address;
    private ProgramStorage _program;

    public InstructionEntry(int address, ProgramStorage program) {
        _address = address;
        _program = program;
    }

    public string AddressDecimal => _address.ToString();
    public string AddressHex => _address.ToString(NumberLiteralFormat.HexPrefix);
    public string Bytecode => Convert.ToString(_program[_address].Bytecode, 2).PadLeft(32, '0');
    public string InstructionString => _program[_address].ToString();
}