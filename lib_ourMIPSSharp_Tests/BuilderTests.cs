using lib_ourMIPSSharp;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp_Tests; 

public class BuilderTests {
    private readonly List<string> _sourceCodesSuccess = new();
    private readonly List<List<uint>> _resultBytecode = new();

    [SetUp]
    public void Setup() {
        // Load mult_philos
        _sourceCodesSuccess.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS"));
        _resultBytecode.Add(File.ReadAllLines("../../../Samples/mult_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
        
        // Load instructiontests_philos
        _sourceCodesSuccess.Add(File.ReadAllText("../../../Samples/instructiontests_philos.ourMIPS"));
        _resultBytecode.Add(File.ReadAllLines("../../../Samples/instructiontests_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
        
        // Load sort_philos
        _sourceCodesSuccess.Add(File.ReadAllText("../../../Samples/sort_philos.ourMIPS"));
        _resultBytecode.Add(File.ReadAllLines("../../../Samples/sort_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
    }

    [Test(Description = "Tests that valid code builds successfully."), Sequential]
    public void TestSuccessfulBuilds(
        [Range(0, 2)]
        int index) {
        var b = new Builder(_sourceCodesSuccess[index], DialectOptions.None);
        Assert.IsTrue(b.FullBuild());

        for (int i = 0; i < _resultBytecode[index].Count; i++) {
            Assert.That(b.Bytecode[i], Is.EqualTo(_resultBytecode[index][i]));
        }
    }
}