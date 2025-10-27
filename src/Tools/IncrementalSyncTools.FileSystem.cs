namespace Maenifold.Tools;

public partial class IncrementalSyncTools
{
    private static void DebounceFileOperation(string path, WatcherChangeTypes changeType, object? eventArgs = null)
    {
        lock (_lock)
        {

            if (_debounceTimers.TryGetValue(path, out var existing))
            {
                existing.timer?.Dispose();
            }


            var timer = new Timer(_ =>
            {
                lock (_lock)
                {
                    _debounceTimers.Remove(path);
                }


                switch (changeType)
                {
                    case WatcherChangeTypes.Created:
                        ProcessFileCreated(path);
                        break;
                    case WatcherChangeTypes.Changed:
                        ProcessFileChanged(path);
                        break;
                    case WatcherChangeTypes.Deleted:
                        ProcessFileDeleted(path);
                        break;
                    case WatcherChangeTypes.Renamed:
                        if (eventArgs is RenamedEventArgs renamed)
                            ProcessFileRenamed(renamed.OldFullPath, path);
                        break;
                }
            }, null, _debounceMs, Timeout.Infinite);

            _debounceTimers[path] = (timer, changeType, eventArgs);
        }
    }

    private static void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains(".git")) return;
        DebounceFileOperation(e.FullPath, WatcherChangeTypes.Created);
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains(".git")) return;
        DebounceFileOperation(e.FullPath, WatcherChangeTypes.Changed);
    }

    private static void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains(".git")) return;
        DebounceFileOperation(e.FullPath, WatcherChangeTypes.Deleted);
    }

    private static void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (e.FullPath.Contains(".git")) return;
        DebounceFileOperation(e.FullPath, WatcherChangeTypes.Renamed, e);
    }

    private static void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Console.Error.WriteLine($"[FileWatcher] Error: {e.GetException().Message}");
    }
}
