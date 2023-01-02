namespace lib_ourMIPSSharp.EmulatorComponents; 

/// <summary>
/// Represents tha main storage (or RAM) of the VM.
/// Keys are memory addresses (word-wise) and values are the int32 words.
/// </summary>
public class MainStorage : Dictionary<int, int> {
    private Random _random = new Random();
    
    public int this[int index] {
        get {
            if (!ContainsKey(index))
                InitializeWord(index);
            return base[index];
        }
        set => base[index] = value;
    }

    private void InitializeWord(int index) {
        base[index] = _random.Next(int.MinValue, int.MaxValue);
    }

    public static MainStorage InitializePhilos(string inputString) {
        /// TODO Implement
        throw new NotImplementedException();
    }

    public static MainStorage InitializeYapjoma(string inputString) {
        /// TODO Implement
        throw new NotImplementedException();
    }
}