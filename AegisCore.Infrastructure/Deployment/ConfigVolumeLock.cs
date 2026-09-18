namespace AegisCore.Infrastructure.Deployment;

/// <summary>
/// Two application replicas share one config volume and start at the same moment. Whoever holds
/// this lock provisions; the other finds the artefacts already there. The lock is a file created
/// with CreateNew (atomic on every filesystem Docker mounts); a lock older than two minutes is a
/// crashed provisioner's and is taken over.
/// </summary>
public sealed class ConfigVolumeLock : IDisposable
{
    private readonly string _path;
    private ConfigVolumeLock(string path) => _path = path;

    public static ConfigVolumeLock Acquire(string directory, TimeSpan? timeout = null)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, ".bootstrap.lock");
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(90));
        while (true)
        {
            try
            {
                using (new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
                return new ConfigVolumeLock(path);
            }
            catch (IOException)
            {
                try { if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddMinutes(-2)) { File.Delete(path); continue; } }
                catch (IOException) { }
                if (DateTime.UtcNow >= deadline) throw new StartupBootstrap.BootstrapException($"Another process has held the config-volume bootstrap lock '{path}' for over 90 seconds. If no other replica is starting, delete the file and start again.");
                Thread.Sleep(200);
            }
        }
    }

    public void Dispose()
    {
        try { File.Delete(_path); } catch (IOException) { }
    }
}
