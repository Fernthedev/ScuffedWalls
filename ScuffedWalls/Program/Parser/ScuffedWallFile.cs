using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ScuffedWalls;

internal class ScuffedWallFile
{
    public ScuffedWallFile(string path)
    {
        Path = path;
        File = new FileInfo(path);
        Detector = new FileChangeDetector(File);
        Refresh();
    }

    public string Path { get; }
    public FileInfo File { get; }
    public List<Parameter> Parameters { get; private set; }
    public FileChangeDetector Detector { get; }
    public List<KeyValuePair<int, string>> Raw { get; private set; }
    public List<KeyValuePair<int, string>> Lines { get; private set; }

    public void Refresh()
    {
        Raw = GetLines();
        Lines = RemoveEmptyLines(RemoveCommentedAreas(Raw));
        Parameters = Lines.ToParameters();

        if (Raw.Any(line => line.Value.ToLower().Contains("rick roll")))
        {
            var process = new ProcessStartInfo
                { FileName = "https://www.youtube.com/watch?v=xvFZjo5PgG0", UseShellExecute = true };
            Process.Start(process);
            Environment.Exit(1);
        }

        if (ScuffedWallsContainer.ScuffedConfig.IsBackupEnabled)
            System.IO.File.Copy(Path,
                System.IO.Path.Combine(ScuffedWallsContainer.ScuffedConfig.BackupPaths.BackupSWFolderPath,
                    $"{DateTime.Now.ToFileString()}.sw"));
    }

    public static List<KeyValuePair<int, string>> RemoveCommentedAreas(IEnumerable<KeyValuePair<int, string>> lines)
    {
        var convertedLines = lines.ToList();

        var isMassComment = false;
        for (var i = 0; i < convertedLines.Count; i++)
            convertedLines[i] =
                new KeyValuePair<int, string>(convertedLines[i].Key, getCommented(convertedLines[i].Value));

        string getCommented(string line)
        {
            var commented = "";
            for (var i = 0; i < line.Length; i++)
            {
                if (i < line.Length - 1 && line[i + 1] == '#' && line[i] == '/') isMassComment = true;
                if (!isMassComment)
                {
                    if (line[i] == '#') break;
                    commented += line[i];
                }

                if (i != 0 && line[i - 1] == '#' && line[i] == '/') isMassComment = false;
            }

            return commented;
        }

        return convertedLines;
    }

    public static List<KeyValuePair<int, string>> RemoveEmptyLines(List<KeyValuePair<int, string>> lines)
    {
        var filtered = new List<KeyValuePair<int, string>>();
        foreach (var line in lines)
            if (!string.IsNullOrEmpty(line.Value.ToLower().RemoveWhiteSpace()))
                filtered.Add(line);
        return filtered;
    }

    public static List<KeyValuePair<int, string>> GetLinesFromFile(string path)
    {
        var raw = new List<KeyValuePair<int, string>>();
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using var FileReader = new StreamReader(stream);
            var i = 1;
            while (!FileReader.EndOfStream)
            {
                raw.Add(new KeyValuePair<int, string>(i, FileReader.ReadLine()));
                i++;
            }
        }

        return raw;
    }

    public List<KeyValuePair<int, string>> GetLines()
    {
        return GetLinesFromFile(Path);
    }
}