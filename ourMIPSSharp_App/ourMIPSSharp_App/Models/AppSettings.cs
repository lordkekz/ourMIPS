using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ourMIPSSharp_App.Models;

public class AppSettings {
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private static string _dataFolder;
    private static string _settingsFilePath;
    private List<OpenScriptBackend> _openFiles;
    public List<string>? OpenFiles { get; set; }
    public string? ActiveTheme { get; set; }

    public AppSettings() { }

    public void SaveSettings() {
        var jsonString = JsonSerializer.Serialize(this, SerializerOptions);
        Directory.CreateDirectory(_dataFolder);
        File.WriteAllText(_settingsFilePath, jsonString);
    }

    private void ApplyDefaultsIfNull() {
        OpenFiles ??= new List<string>();
        // ActiveTheme ??= ""
    }

    public static AppSettings MakeInstance() {
        _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create),
            "OurMIPS");
        _settingsFilePath = Path.Combine(_dataFolder, "settings.json");
        AppSettings result;

        try {
            var jsonString = File.ReadAllText(_settingsFilePath);
            result = JsonSerializer.Deserialize<AppSettings>(jsonString);
            result ??= new AppSettings();
            result.ApplyDefaultsIfNull();
            return result;
        }
        catch (Exception ex) {
            Console.Error.WriteLine(ex);
        }

        result = new AppSettings();
        result.ApplyDefaultsIfNull();
        return result;
    }
}