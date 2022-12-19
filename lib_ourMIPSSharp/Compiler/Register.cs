using System.Linq;

namespace lib_ourMIPSSharp; 

public enum Register {
    Zero, At, V0, V1, A0, A1, A2, A3,
    T0, T1, T2, T3, T4, T5, T6, T7,
    S0, S1, S2, S3, S4, S5, S6, S7,
    T8, T9, K0, K1, Gp, Sp, S8, Ra
}

public static class RegisterHelper {
    public static Register FromString(string str) {
        str = str.ToLowerInvariant();
        if (str.StartsWith("$"))
            str = str.Substring(1);
        
        Register result;
        if (Register.TryParse(str, out result))
            return result;

        if (str.StartsWith("r["))
            str = str.Substring(2, str.Length - 3);
        if (str.StartsWith("r"))
            str = str.Substring(1);
        
        result = (Register)int.Parse(str);
        return result;
    }
}