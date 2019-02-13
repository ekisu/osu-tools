using System;
using System.IO;

class RemoveUnneededBuildFiles {
    static string[] RemovableFiles = new string[] {
        "PerformanceCalculator.deps.json",
        "Humanizer.dll",
        "af",
        "ar",
        "bg",
        "bn-BD",
        "cs",
        "da",
        "de",
        "el",
        "es",
        "fa",
        "fi-FI",
        "fr",
        "fr-BE",
        "he",
        "hr",
        "hu",
        "id",
        "it",
        "ja",
        "ko",
        "lv",
        "ms-MY",
        "mt",
        "nb",
        "nb-NO",
        "nl",
        "pl",
        "pt",
        "pt-BR",
        "ro",
        "ru",
        "sk",
        "sl",
        "sr",
        "sr-Latn",
        "sv",
        "tr",
        "uk",
        "uz-Cyrl-UZ",
        "uz-Latn-UZ",
        "vi",
        "zh-CN",
        "zh-Hans",
        "zh-Hant",
        "x64",
        "x86",
        "libbass*",
        "ManagedBass*",
        "osu.Game.Resources.dll",
        "SQLite*",
        "e_sqlite3.dll",
        "Sharp*",
        "SixLabors*",
        "FFMpeg.AutoGen.dll"
    };

    static void Main(string[] args) {
        var buildDir = args[0];

        foreach (var line in RemovableFiles) {
            var path = Path.Combine(buildDir, line);
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            } else {
                // Attempt Glob
                var di = new DirectoryInfo(buildDir);
                foreach (var file in di.EnumerateFiles(line)) {
                    file.Delete();
                }
            }
        }
    }
}