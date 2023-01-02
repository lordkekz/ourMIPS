using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp_Tests;

public class RegisterTests {
    [SetUp]
    public void Setup() { }

    [Test(Description = "Tests that RegisterHelper.FromString parses valid register descriptors correctly."), Sequential]
    public void TestRegisterHelperValid(
        [Values("zero", "$at", "$r0", "r01", "R[0]", "r[00]", "$R[31]")]
        string str,
        [Values(Register.Zero, Register.At, Register.Zero, Register.At, Register.Zero, Register.Zero, Register.Ra)]
        Register expected) {
        Assert.AreEqual(expected, RegisterHelper.FromString(str));
    }

    [Test(Description = "Tests that RegisterHelper.FromString returns None for invalid register descriptors.")]
    public void TestRegisterHelperInvalid(
        [Values("abcdefg", "$$", "reg0", "r[]", "$r[4", "r40", "none")]
        string str) {
        Assert.AreEqual(Register.None, RegisterHelper.FromString(str));
    }
}