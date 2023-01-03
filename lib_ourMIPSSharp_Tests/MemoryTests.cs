using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using NUnit.Framework;

namespace lib_ourMIPSSharp_Tests;

[TestFixture]
public class MemoryTests {
    private readonly List<string> _memoryPhilos = new();
    private readonly List<string> _memoryYapjoma = new();
    private readonly List<string> _textOutputs = new();

    [SetUp]
    public void Setup() {
        // files
        _memoryPhilos.Add(File.ReadAllText("../../../Samples/memory_init1_philos"));
        // _memoryPhilos.Add(File.ReadAllText("../../../Samples/memory_init2_philos"));
        // _memoryYapjoma.Add(File.ReadAllText("../../../Samples/memory_init1_yapjoma"));
        // _memoryYapjoma.Add(File.ReadAllText("../../../Samples/memory_init2_yapjoma"));
        _textOutputs.Add(File.ReadAllText("../../../Samples/memory_init1_result")
            .Replace("\r", ""));
        // _textOutputs.Add(File.ReadAllText("../../../Samples/memory_init2_result").Replace("\r", ""));
    }

    [Test(Description = "Tests memory initialization for philos"), Sequential]
    public void TestInitializePhilos(
        [Range(0, 1)] int index) {
        var mem = new MainStorage();
        mem.InitializePhilos(_memoryPhilos[index]);
        var str = string.Join('\n', mem.ToArray());
        Assert.That(str, Is.EqualTo(_textOutputs[index]));
    }

    // TODO reactivate test once implemented
    // [Test(Description = "Tests memory initialization for yapjoma"), Sequential]
    // public void TestInitializeYapjoma(
    //     [Range(0, 1)] int index) {
    //     var mem = new MainStorage();
    //     mem.InitializeYapjoma(_memoryYapjoma[index]);
    //     var str = string.Join('\n', mem.ToArray());
    //     Assert.That(str, Is.EqualTo(_textOutputs[index]));
    // }

    [Test(Description = "Tests FromToken and Matches for valid/true values"), Sequential]
    public void TestRandomInitializedRegisters() {
        var reg1 = new RegisterStorage();
        reg1.ProgramCounter++;
        reg1.FlagOverflow = !reg1.FlagOverflow;
        Assert.That(reg1[Register.Sp], Is.EqualTo(int.MaxValue));
        
        var reg2 = new RegisterStorage();
        reg2[Register.Zero] = -62;
        Assert.That(reg2[Register.Zero], Is.EqualTo(0));
        Assert.That(reg2[Register.Sp], Is.EqualTo(int.MaxValue));

        foreach (var r in Enum.GetValues<Register>()) {
            if (r is not Register.Sp and not Register.Zero and not Register.None)
                Assert.That(reg1[r], Is.Not.EqualTo(reg2[r]));
        }
    }

    [Test(Description = "Tests FromToken and Matches for valid/true values"), Sequential]
    public void TestRandomInitializedMemory([Random(100)] int address) {
        var mem1 = new MainStorage();
        var mem2 = new MainStorage();
        Assert.That(mem1[address], Is.Not.EqualTo(mem2[address]));
    }
}