using lib_ourMIPSSharp;

namespace lib_ourMIPSSharp_Tests.Compiler; 

public class BuilderTests {
    private readonly List<string> _sourceCodesSuccess = new();

    [SetUp]
    public void Setup() {
        _sourceCodesSuccess.Add(File.ReadAllText("../../../success_mult_philos.ourMIPS"));
    }

    [Test(Description = "Tests that valid code builds successfully."), Sequential]
    public void TestSuccessfulBuilds(
        [Range(0, 0)]
        int index) {
        var b = new Builder(_sourceCodesSuccess[index], DialectOptions.None);
        Assert.IsTrue(b.FullBuild());
    }
}