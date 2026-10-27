namespace HoldItTogether.Services.Extensions;

/// <summary>Waits for a stable file; installation itself remains serialized by the store.</summary>
public sealed class ExtensionInbox(ExtensionStore store, ExtensionUi ui, ExtensionActivationService activation) : IAsyncDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _worker;
    public void Start() => _worker ??= RunAsync(_lifetime.Token);
    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        var observed = new Dictionary<string, (long Length, DateTime Modified)>();
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    var files = Directory.EnumerateFiles(store.Inbox, "*.hitmenu").Take(100).ToArray();
                    foreach (var old in observed.Keys.Except(files).ToArray()) observed.Remove(old);
                    foreach (var file in files)
                    {
                        var info = new FileInfo(file); var stamp = (info.Length, info.LastWriteTimeUtc);
                        if (!observed.TryGetValue(file, out var previous) || previous != stamp) { observed[file] = stamp; continue; }
                        try
                        {
                            InstalledExtension installed;
                            await using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                                installed = await store.InstallAsync(stream, info.Name, token);
                            var processed = Path.Combine(store.Inbox, "Processed"); Directory.CreateDirectory(processed);
                            File.Move(file, Path.Combine(processed, Guid.NewGuid().ToString("N") + "-" + info.Name));
                            observed.Remove(file);
                            ui.Open();
                            await activation.ActivateAsync(installed.Manifest.Id, installed.Manifest.Name);
                        }
                        catch (IOException) { /* Copy may still be in progress. */ }
                        catch (Exception error) when (error is not OperationCanceledException)
                        {
                            var rejected = Path.Combine(store.Inbox, "Rejected"); Directory.CreateDirectory(rejected);
                            File.Move(file, Path.Combine(rejected, Guid.NewGuid().ToString("N") + "-" + info.Name));
                            ui.Report("Installation failed: " + error.GetBaseException().Message, true);
                        }
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { /* Retry after transient filesystem errors. */ }
            }
        }
        catch (OperationCanceledException) { }
    }
    public async ValueTask DisposeAsync()
    { _lifetime.Cancel(); if (_worker is not null) await _worker; _lifetime.Dispose(); }
}
