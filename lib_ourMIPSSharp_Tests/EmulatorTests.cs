using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using NUnit.Framework;
using lib_ourMIPSSharp.EmulatorComponents;

namespace lib_ourMIPSSharp_Tests;

[TestFixture]
public class EmulatorTests {
    private readonly List<List<uint>> _resultBytecode = new();
    
    [SetUp]
    public void Setup() {
        _resultBytecode.Add(File.ReadAllLines("../../../Samples/mult_philos.ourMIPS.bytecode")
            .Select(line => Convert.ToUInt32(line, 2)).ToList());
        
    }
    
    [Test(Description = "Tests that valid code builds successfully."), Sequential]
    public void TestEmulatorResults(
        [Range(0, 0)]
        int index) {
        
    }
}