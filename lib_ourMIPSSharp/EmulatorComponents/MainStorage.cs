using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Nodes;
using lib_ourMIPSSharp.CompilerComponents.Elements;

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

    /// <summary>
    /// Initializes the MainStorage with the values from the given test environment using the given id.
    /// If no id is given and the json string contains only one environment, that environment is used.
    /// Otherwise the id is mandatory.
    /// </summary>
    /// <param name="inputString"></param>
    /// <param name="id"></param>
    /// <returns>whether initialization was successful</returns>
    public bool InitializePhilos(string inputString, string? id = null) {
        // TODO add/improve exception handling

        try {
            var rootNode = JsonNode.Parse(inputString)!.AsObject();
            JsonObject testEnv;
            if (id is null) {
                if (rootNode.Count != 1)
                    return false;
                testEnv = rootNode.First().Value.AsObject();
            }
            else
                testEnv = rootNode[id].AsObject();

            var mem_init = testEnv["entry_mem"].AsObject();
            foreach (var pair in mem_init) {
                var address = NumberLiteral.ParseString(pair.Key);
                this[address] = pair.Value.GetValue<int>();
            }
        }
        catch (Exception ex) {
            return false;
        }

        return true;
    }

    public JsonObject ToJsonObject(string name = "Default Environment") {
        var testEnv = new JsonObject();
        testEnv["name"] = name;
        var memory = new JsonObject();
        testEnv["entry_mem"] = memory;
        foreach (var pair in this.Order())
            memory[pair.Key.ToString(NumberLiteralFormat.HexPrefix)] = pair.Value;

        return testEnv;
    }

    public void InitializeYapjoma(string inputString) {
        /// TODO Implement
        throw new NotImplementedException();
    }
}