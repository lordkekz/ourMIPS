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

public class RegisterEntry {
    private Register _register;
    private RegisterStorage _registers;

    public RegisterEntry(Register register, RegisterStorage registers) {
        _register = register;
        _registers = registers;
    }

    public string RegId => ((int)_register).ToString();
    public string Name => _register.ToString().ToLower();
    public string ValueDecimal => _registers[_register].ToString(NumberLiteralFormat.Decimal);
    public string ValueHex => _registers[_register].ToString(NumberLiteralFormat.HexPrefix);
    public string ValueBinary => _registers[_register].ToString(NumberLiteralFormat.BinaryPrefix);
}

public class MemoryEntry {
    private int _address;
    private MainStorage _ram;

    public MemoryEntry(int address, MainStorage memory) {
        _address = address;
        _ram = memory;
    }

    public string AddressDecimal => _address.ToString();
    public string AddressHex => _address.ToString(NumberLiteralFormat.HexPrefix);
    public string ValueDecimal => _ram[_address].ToString(NumberLiteralFormat.Decimal);
    public string ValueHex => _ram[_address].ToString(NumberLiteralFormat.HexPrefix);
    public string ValueBinary => _ram[_address].ToString(NumberLiteralFormat.BinaryPrefix);
}