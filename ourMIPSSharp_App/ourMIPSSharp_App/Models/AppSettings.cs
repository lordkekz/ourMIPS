using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace ourMIPSSharp_App.Models;

public class AppSettings {
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private static string _dataFolder;
    private static string _settingsFilePath;
    private List<FileBackend> _openFiles;
    public List<string>? OpenFiles { get; set; }
    public MyAppTheme? ActiveTheme { get; set; }
    public CompilerMode? ActiveCompilerMode { get; set; }

    public AppSettings() { }

    public void SaveSettings() {
        var jsonString = JsonSerializer.Serialize(this, SerializerOptions);
        Directory.CreateDirectory(_dataFolder);
        File.WriteAllText(_settingsFilePath, jsonString);
    }

    private void ApplyDefaultsIfNull() {
        OpenFiles ??= new List<string>();
        ActiveTheme ??= MyAppTheme.Dark;
        ActiveCompilerMode ??= CompilerMode.OurMIPSSharp;
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

public enum CompilerMode {
    OurMIPSSharp,
    Philosonline,
    Yapjoma
}

public static class CompilerModeExtensions {
    public static DialectOptions ToDialectOptions(this CompilerMode mode) {
        return mode switch {
            CompilerMode.OurMIPSSharp => DialectOptions.None,
            CompilerMode.Philosonline => DialectOptions.Philosonline,
            CompilerMode.Yapjoma => DialectOptions.Yapjoma,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}

public enum MyAppTheme {
    Dark, Light, HelloKitty
}