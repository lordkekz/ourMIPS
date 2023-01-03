using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using NUnit.Framework;
using lib_ourMIPSSharp.EmulatorComponents;

namespace lib_ourMIPSSharp_Tests;

[TestFixture]
public class EmulatorTests {
    private readonly List<List<uint>> _bytecode = new();
    private readonly List<string> _stringConstants = new();
    private readonly List<string> _textInputs = new();
    private readonly List<string> _textOutputs = new();
    private readonly List<string> _envs = new();

    [SetUp]
    public void Setup() {
        // Load mult_philos (sample 1)
        _bytecode.Add(File.ReadAllLines("../../../Samples/mult_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
        _stringConstants.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS.strings")
            .Replace("\\0", "\0"));
        _textInputs.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS.input1"));
        _textOutputs.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS.output1"));
        _envs.Add(null);
        
        // Load mult_philos (sample 2)
        _bytecode.Add(File.ReadAllLines("../../../Samples/mult_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
        _stringConstants.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS.strings")
            .Replace("\\0", "\0"));
        _textInputs.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS.input2"));
        _textOutputs.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS.output2"));
        _envs.Add(null);
        
        // Load instructiontests_philos
        _bytecode.Add(File.ReadAllLines("../../../Samples/instructiontests_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
        _stringConstants.Add(File.ReadAllText("../../../Samples/instructiontests_philos.ourMIPS.strings")
            .Replace("\\0", "\0"));
        _textInputs.Add("");
        _textOutputs.Add(File.ReadAllText("../../../Samples/instructiontests_philos.ourMIPS.output"));
        _envs.Add(File.ReadAllText("../../../Samples/instructiontests_philos.ourMIPS.ram.json"));
        
        // Load sort_philos
        _bytecode.Add(File.ReadAllLines("../../../Samples/sort_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
        _stringConstants.Add(File.ReadAllText("../../../Samples/sort_philos.ourMIPS.strings")
            .Replace("\\0", "\0"));
        _textInputs.Add("");
        _textOutputs.Add(File.ReadAllText("../../../Samples/sort_philos.ourMIPS.output"));
        _envs.Add(File.ReadAllText("../../../Samples/sort_philos.ourMIPS.ram.json"));
    }

    [Test(Description = "Tests that the emulator produces the expected outputs."), Combinatorial]
    public void TestEmulatorResults(
        [Range(0, 3)] int index) {
        var e = new Emulator(_bytecode[index], _stringConstants[index]) {
            TextIn = new StringReader(_textInputs[index]),
            TextOut = new StringWriter()
        };

        if (_envs[index] is not null)
            e.Memory.InitializePhilos(_envs[index]);
        
        e.RunUntilTerminated(1000000);
        Assert.That(e.Terminated);
        e.TextOut.Flush();
        Console.WriteLine(e.TextOut);
        Assert.That(e.TextOut.ToString(), Is.EqualTo(_textOutputs[index]));
    }
}