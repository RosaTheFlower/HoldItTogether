using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

namespace HoldItTogether.Services.Extensions;

/// <summary>Installs bytes only. Import never executes a package.</summary>
public sealed class ExtensionStore
{
    public const long MaxPackageBytes = 64 * 1024 * 1024;
    public const long MaxExtractedBytes = 128 * 1024 * 1024;
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1);
    private readonly object _sync = new();
    private ExtensionRegistry _state = new();
    public string Root { get; }
    public string Inbox => Path.Combine(Root, "Inbox");
    public ExtensionRegistry State => Volatile.Read(ref _state);
    public string? Error { get; private set; }
    public event Action? Changed;
    public Func<ExtensionManifest, ExtensionManifest, CancellationToken, Task<bool>>? ConfirmDowngradeAsync { get; set; }
    public ExtensionStore() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoldItTogether", "Modules")) { }
    public ExtensionStore(string root)
    {
        Root = Path.GetFullPath(root);
        foreach (var folder in new[] { "Inbox", "Installed", "Staging", "State", "Logs" }) Directory.CreateDirectory(Path.Combine(Root, folder));
        var path = Path.Combine(Root, "registry.json");
        try
        {
            if (File.Exists(path))
            {
                if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Extension registry is too large.");
                var state = JsonSerializer.Deserialize<ExtensionRegistry>(File.ReadAllText(path), Json) ?? new();
                if (state.Packages is null || state.Packages.Length > 100 || state.Order is null || state.DisabledMenus is null) throw new InvalidDataException("Invalid extension registry.");
                foreach (var package in state.Packages) package.Manifest.Validate();
                _state = state with { Order = state.Order.Select(NormalizeMenuId).Distinct().ToArray(), DisabledMenus = state.DisabledMenus.Select(NormalizeMenuId).Distinct().ToArray() };
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or ArgumentException or NullReferenceException)
        { Error = "The extension registry could not be read. Existing files were preserved."; }
    }
    public string PackagePath(ExtensionManifest manifest) => Path.Combine(Root, "Installed", manifest.Id, manifest.Version);
    public bool IsEnabled(string menuId) => !State.DisabledMenus.Contains(menuId, StringComparer.Ordinal);
    // Migrate historical serialized CLR names while keeping all current source identifiers English.
    private static string NormalizeMenuId(string id) => id switch
    {
        "HoldItTogether.Components.HitMenus.HitMenu\u0043onfiguracoes" => HitMenus.HitMenuCatalog.SettingsId,
        "HoldItTogether.Components.HitMenus.HitMenu\u0052elogio" => "HoldItTogether.Components.HitMenus.HitMenuClock",
        _ => id
    };

    private void Update(Func<ExtensionRegistry, ExtensionRegistry> change)
    {
        lock (_sync)
        {
            var next = change(_state);
            var path = Path.Combine(Root, "registry.json");
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(next, Json));
            File.Move(temporary, path, true);
            Volatile.Write(ref _state, next);
        }
        Changed?.Invoke();
    }
    public void SetEnabled(string id, bool enabled)
    {
        if (id == HitMenus.HitMenuCatalog.SettingsId) return;
        Update(state => state with { DisabledMenus = enabled ? state.DisabledMenus.Where(value => value != id).ToArray() : state.DisabledMenus.Append(id).Distinct().ToArray() });
    }
    public void SetOrder(IEnumerable<string> ids) => Update(state => state with { Order = ids.Distinct(StringComparer.Ordinal).Take(2100).ToArray() });
    public void Remove(string id) => Update(state => state with { Packages = state.Packages.Select(item => item.Manifest.Id == id ? item with { PendingDelete = true } : item).ToArray() });
    public async Task<ExtensionCleanupResult> ClearOldVersionsAsync(IReadOnlyDictionary<string,string> loadedVersions, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            return await Task.Run(() =>
            {
                int removed = 0, skipped = 0;
                var clearedPrevious = new HashSet<string>();
                var removedPackages = new HashSet<string>();
                lock (_sync)
                {
                    if (HasLink(ExtensionManifest.SafePath(Root,"Installed"))) return new ExtensionCleanupResult(0,1);
                    try
                    {
                        foreach (var package in _state.Packages)
                        {
                            token.ThrowIfCancellationRequested();
                            var directory = ExtensionManifest.SafePath(Root, "Installed/" + package.Manifest.Id);
                            if (!Directory.Exists(directory)) { if (package.PendingDelete && !loadedVersions.ContainsKey(package.Manifest.Id)) removedPackages.Add(package.Manifest.Id); continue; }
                            if (HasLink(directory)) { skipped++; continue; }
                            foreach (var path in Directory.EnumerateDirectories(directory))
                            {
                                token.ThrowIfCancellationRequested();
                                var version = Path.GetFileName(path);
                                if (!Version.TryParse(version, out _) || (!package.PendingDelete && version == package.Manifest.Version)) continue;
                                if (version == loadedVersions.GetValueOrDefault(package.Manifest.Id)) { if (package.PendingDelete) skipped++; continue; }
                                try
                                {
                                    if (HasLinkedDescendant(path)) { skipped++; continue; }
                                    DeleteTree(path); removed++;
                                    if (version == package.PreviousVersion) clearedPrevious.Add(package.Manifest.Id);
                                }
                                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { skipped++; }
                            }
                        }
                            foreach (var package in _state.Packages.Where(package => package.PendingDelete && !loadedVersions.ContainsKey(package.Manifest.Id)))
                            {
                                var directory = ExtensionManifest.SafePath(Root, "Installed/" + package.Manifest.Id);
                                if (Directory.Exists(directory) && !HasLink(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                                { DeleteTree(directory); removedPackages.Add(package.Manifest.Id); }
                            }
                    }
                    finally
                    {
                        // A canceled view must not leave a rollback pointer to files already removed.
                        if (clearedPrevious.Count > 0 || removedPackages.Count > 0)
                            Update(state => state with { Packages = state.Packages.Where(package => !removedPackages.Contains(package.Manifest.Id)).Select(package => clearedPrevious.Contains(package.Manifest.Id) ? package with { PreviousVersion = null } : package).ToArray() });
                    }
                }
                return new ExtensionCleanupResult(removed, skipped);
            },token);
        }
        finally { _gate.Release(); }
    }
    private static bool HasLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private static bool HasLinkedDescendant(string root)
    {
        if (HasLink(root)) return true;
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.TryPop(out var directory))
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0) return true;
                if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
            }
        return false;
    }

    public void RestorePrevious(string id)
    {
        var current = State.Packages.FirstOrDefault(item => item.Manifest.Id == id) ?? throw new InvalidOperationException("Extension not found.");
        if (current.PreviousVersion is null || !System.Version.TryParse(current.PreviousVersion, out _)) throw new InvalidOperationException("No previous version is available.");
        var file = Path.Combine(Root, "Installed", id, current.PreviousVersion, "manifest.json");
        var manifest = JsonSerializer.Deserialize<ExtensionManifest>(File.ReadAllText(file), Json) ?? throw new InvalidDataException("Invalid previous version.");
        manifest.Validate();
        if (manifest.Id != id || manifest.Version != current.PreviousVersion) throw new InvalidDataException("Previous version identity mismatch.");
        Update(state => state with { Packages = state.Packages.Select(item => item.Manifest.Id == id ? new InstalledExtension(manifest, "", false, current.Manifest.Version) : item).ToArray() });
    }

    public void FinishPendingDeletes()
    {
        foreach (var item in State.Packages.Where(item => item.PendingDelete).ToArray())
        {
            try
            {
                DeleteTree(Path.Combine(Root, "Installed", item.Manifest.Id));
                Update(state => state with { Packages = state.Packages.Where(value => value.Manifest.Id != item.Manifest.Id).ToArray() });
            }
            catch (IOException) { Error = "Some extension files are in use. Removal will be retried next time."; }
            catch (UnauthorizedAccessException) { Error = "Some extension files could not be removed."; }
        }
    }
    private void DeleteTree(string path)
    {
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid cleanup directory.");
        if (Directory.Exists(full)) Directory.Delete(full, true);
    }

    public async Task<InstalledExtension> InstallAsync(Stream source, string filename, CancellationToken token = default)
    {
        if (!filename.EndsWith(".hitmenu", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose a .hitmenu package.");
        await _gate.WaitAsync(token);
        var staging = Path.Combine(Root, "Staging", Guid.NewGuid().ToString("N"));
        var archivePath = Path.Combine(staging, "package.zip");
        var content = Path.Combine(staging, "content");
        try
        {
            Directory.CreateDirectory(staging);
            await using (var output = File.Create(archivePath)) await CopyLimited(source, output, MaxPackageBytes, token);
            string hash;
            await using (var file = File.OpenRead(archivePath)) hash = Convert.ToHexString(await SHA256.HashDataAsync(file, token));
            using var archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count is < 2 or > 2000) throw new InvalidDataException("Invalid number of package files.");
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                token.ThrowIfCancellationRequested();
                var relative = entry.FullName.TrimEnd('/');
                var destination = ExtensionManifest.SafePath(content, relative);
                var mode = (entry.ExternalAttributes >> 16) & 0xF000;
                if ((mode != 0 && mode is not (0x8000 or 0x4000)) || (entry.ExternalAttributes & 0x400) != 0 || !paths.Add(relative))
                    throw new InvalidDataException("Links and duplicate package paths are not supported.");
                total = checked(total + entry.Length);
                if (total > MaxExtractedBytes) throw new InvalidDataException("The extracted package exceeds 128 MB.");
                if (entry.FullName.EndsWith('/')) { Directory.CreateDirectory(destination); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await using var input = entry.Open();
                await using var output = File.Create(destination);
                await CopyLimited(input, output, entry.Length, token);
            }
            var manifestPath = Path.Combine(content, "manifest.json");
            if (!File.Exists(manifestPath) || new FileInfo(manifestPath).Length > 65536) throw new InvalidDataException("Missing or oversized manifest.json.");
            var manifest = JsonSerializer.Deserialize<ExtensionManifest>(await File.ReadAllTextAsync(manifestPath, token), Json) ?? throw new InvalidDataException("Invalid manifest.");
            manifest.Validate();
            foreach (var file in manifest.Menus.Select(item => item.ComponentAssembly).Append(manifest.EntryAssembly).Distinct())
            {
                using var stream = File.OpenRead(ExtensionManifest.SafePath(content, file));
                using var reader = new PEReader(stream);
                if (!reader.HasMetadata || !reader.GetMetadataReader().IsAssembly) throw new InvalidDataException("A declared component is not a .NET assembly.");
            }
            foreach (var style in manifest.Menus.SelectMany(item => item.Styles))
            {
                var info = new FileInfo(ExtensionManifest.SafePath(content, style));
                if (!info.Exists || info.Length > 1024 * 1024) throw new InvalidDataException("Missing or oversized menu stylesheet.");
            }
            var existing = State.Packages.FirstOrDefault(item => item.Manifest.Id == manifest.Id);
            if (existing?.Hash == hash && !existing.PendingDelete) return existing;
            if (existing is not null && !existing.PendingDelete)
            {
                var comparison = System.Version.Parse(manifest.Version).CompareTo(System.Version.Parse(existing.Manifest.Version));
                if (comparison == 0) throw new InvalidDataException("Changed package contents require a different version number.");
                if (comparison < 0 && (ConfirmDowngradeAsync is null || !await ConfirmDowngradeAsync(existing.Manifest, manifest, token)))
                    throw new OperationCanceledException("Extension downgrade canceled.", token);
            }
            if (existing is null && State.Packages.Length >= 100) throw new InvalidDataException("The extension limit has been reached.");
            var destinationRoot = PackagePath(manifest);
            var retained = Directory.Exists(destinationRoot);
            if (retained)
            {
                if (HasLinkedDescendant(destinationRoot)) throw new InvalidDataException("Invalid retained package directory.");
                var incoming = Directory.GetFiles(content, "*", SearchOption.AllDirectories);
                if (incoming.Length != Directory.GetFiles(destinationRoot, "*", SearchOption.AllDirectories).Length || incoming.Any(file =>
                    !File.Exists(Path.Combine(destinationRoot, Path.GetRelativePath(content, file))) ||
                    !SameFile(file, Path.Combine(destinationRoot, Path.GetRelativePath(content, file)))))
                    throw new InvalidDataException("This version is retained with different contents. Clear old versions before importing it.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destinationRoot)!);
            if (!retained) Directory.Move(content, destinationRoot);
            var installed = new InstalledExtension(manifest, hash, PreviousVersion: existing?.Manifest.Version);
            try { Update(state => state with { Packages = state.Packages.Where(item => item.Manifest.Id != manifest.Id).Append(installed).ToArray() }); }
            catch { if (!retained) DeleteTree(destinationRoot); throw; }
            return installed;
        }
        finally { try { DeleteTree(staging); } finally { _gate.Release(); } }
    }
    private static bool SameFile(string left, string right)
    {
        if (new FileInfo(left).Length != new FileInfo(right).Length) return false;
        using var first = File.OpenRead(left); using var second = File.OpenRead(right);
        return SHA256.HashData(first).AsSpan().SequenceEqual(SHA256.HashData(second));
    }
    private static async Task CopyLimited(Stream input, Stream output, long limit, CancellationToken token)
    {
        var buffer = new byte[81920]; long size = 0; int read;
        while ((read = await input.ReadAsync(buffer, token)) != 0)
        {
            size += read;
            if (size > limit) throw new InvalidDataException("The package exceeds the allowed size.");
            await output.WriteAsync(buffer.AsMemory(0, read), token);
        }
    }
}
