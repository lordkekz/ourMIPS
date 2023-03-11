#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using lib_ourMIPSSharp.CompilerComponents.Elements;

#endregion

namespace ourMIPS_App.Models;

public class AppSettings {
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private static string _dataFolder;
    private static string _settingsFilePath;
    private List<FileBackend> _openFiles;
    public List<string>? OpenFiles { get; set; }
    public MyAppTheme? ActiveTheme { get; set; }
    public int? DialectOpts { get; set; }

    public void SaveSettings() {
        var jsonString = JsonSerializer.Serialize(this, SerializerOptions);
        Directory.CreateDirectory(_dataFolder);
        File.WriteAllText(_settingsFilePath, jsonString);
    }

    private void ApplyDefaultsIfNull() {
        OpenFiles ??= new List<string>();
        ActiveTheme ??= MyAppTheme.Dark;
        DialectOpts ??= 0;
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
    Yapjoma,
    Custom
}

public static class CompilerModeExtensions {
    public static DialectOptions ToDialectOptions(this CompilerMode mode) {
        return mode switch {
            CompilerMode.OurMIPSSharp => DialectOptions.None,
            CompilerMode.Philosonline => DialectOptions.Philosonline,
            CompilerMode.Yapjoma => DialectOptions.Yapjoma,
            CompilerMode.Custom => DialectOptions.None,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public static CompilerMode ToCompilerMode(this DialectOptions opts) {
        return opts switch {
            DialectOptions.None => CompilerMode.OurMIPSSharp,
            DialectOptions.Philosonline => CompilerMode.Philosonline,
            DialectOptions.Yapjoma => CompilerMode.Yapjoma,
            _ => CompilerMode.Custom
        };
    }
}

public enum MyAppTheme {
    Dark,
    Light,
    HelloKitty
}