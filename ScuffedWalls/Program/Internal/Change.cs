using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

internal class FileChangeDetector
{
    private DateTime _lastModifiedTime;
    public FileInfo File;

    public FileChangeDetector(FileInfo file)
    {
        File = file;
        _lastModifiedTime = _currentModifiedTime;
        LatestMessage = "Program opened";
    }

    public static string LatestMessage { get; private set; }
    private DateTime _currentModifiedTime => System.IO.File.GetLastWriteTime(File.FullName);

    public bool HasChanged()
    {
        if (_currentModifiedTime.ToString() == _lastModifiedTime.ToString()) return false;

        _lastModifiedTime = _currentModifiedTime;
        LatestMessage = $"{File.Name} modified";
        return true;
    }

    public void SetChanged()
    {
        File.Refresh();
        _lastModifiedTime = File.LastWriteTime;
    }

    public static void WaitForChange(IEnumerable<FileChangeDetector> files)
    {
        FileChangeDetector changed = null;
        while (!(anyChanged(files, out changed) || RefreshPressed())) Thread.Sleep(100);
        if (changed != null) changed.WaitForUnlock(); //fix vscode bug
    }

    private static bool anyChanged(IEnumerable<FileChangeDetector> files, out FileChangeDetector changed)
    {
        changed = null;
        foreach (var file in files)
            if (file.HasChanged())
            {
                changed = file;
                return true;
            }

        return false;
    }

    public static bool RefreshPressed()
    {
        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.R)
        {
            LatestMessage = "Refreshed";
            return true;
        }

        return false;
    }

    public bool IsLocked()
    {
        try
        {
            System.IO.File.Open(File.FullName, FileMode.Open).Close();
        }
        catch
        {
            return true;
        }

        return false;
    }

    public void WaitForUnlock()
    {
        while (IsLocked()) Thread.Sleep(100);
    }
}