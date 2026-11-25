using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace HoldItTogether.Services.Themes;

/// <summary>Enumerates installed font families on demand; no font files leave the machine.</summary>
public sealed class HitFontCatalog
{
    private readonly object _gate = new();
    private readonly Func<IReadOnlyList<string>> _read;
    private Task<IReadOnlyList<string>>? _pending;

    public HitFontCatalog(Func<IReadOnlyList<string>>? read = null) => _read = read ?? ReadInstalled;

    public Task<IReadOnlyList<string>> GetAsync(CancellationToken token = default, bool refresh = false)
    {
        token.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_pending is null || _pending.IsFaulted || (refresh && _pending.IsCompleted))
                _pending = Task.Run<IReadOnlyList<string>>(() => Array.AsReadOnly(_read()
                    .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase).ToArray()));
            return _pending.WaitAsync(token);
        }
    }

    private static IReadOnlyList<string> ReadInstalled()
    {
        if (!OperatingSystem.IsWindows()) return [];
        var iid = typeof(IDWriteFactory).GUID;
        Marshal.ThrowExceptionForHR(DWriteCreateFactory(0, ref iid, out var factory));
        IDWriteFontCollection? collection = null;
        try
        {
            factory.GetSystemFontCollection(out collection, true);
            var result = new List<string>();
            for (uint i = 0; i < collection.GetFontFamilyCount(); i++)
            {
                collection.GetFontFamily(i, out var family);
                IDWriteLocalizedStrings? names = null;
                try
                {
                    family.GetFamilyNames(out names);
                    names.FindLocaleName(CultureInfo.CurrentUICulture.Name, out var index, out var exists);
                    if (!exists) names.FindLocaleName("en-us", out index, out exists);
                    if (!exists) index = 0;
                    names.GetStringLength(index, out var length);
                    var value = new StringBuilder(checked((int)length + 1));
                    names.GetString(index, value, length + 1);
                    result.Add(value.ToString());
                }
                finally { if (names is not null) Marshal.ReleaseComObject(names); Marshal.ReleaseComObject(family); }
            }
            return result;
        }
        finally { if (collection is not null) Marshal.ReleaseComObject(collection); Marshal.ReleaseComObject(factory); }
    }

    [DllImport("dwrite.dll", ExactSpelling = true)]
    private static extern int DWriteCreateFactory(uint factoryType, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IDWriteFactory factory);

    // Only the used prefix of each interface is declared. Keep inherited vtable slots in order.
    [ComImport, Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDWriteFactory
    {
        void GetSystemFontCollection(out IDWriteFontCollection collection, [MarshalAs(UnmanagedType.Bool)] bool checkForUpdates);
    }
    [ComImport, Guid("a84cee02-3eea-4eee-a827-87c1a02a0fcc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDWriteFontCollection
    {
        [PreserveSig] uint GetFontFamilyCount();
        void GetFontFamily(uint index, out IDWriteFontFamily family);
    }
    [ComImport, Guid("da20d8ef-812a-4c43-9802-62ec4abd7add"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDWriteFontFamily
    {
        void GetFontCollection(out IDWriteFontCollection collection);
        [PreserveSig] uint GetFontCount();
        void GetFont(uint index, out nint font);
        void GetFamilyNames(out IDWriteLocalizedStrings names);
    }
    [ComImport, Guid("08256209-099a-4b34-b86d-c22b110e7771"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDWriteLocalizedStrings
    {
        [PreserveSig] uint GetCount();
        void FindLocaleName([MarshalAs(UnmanagedType.LPWStr)] string locale, out uint index, [MarshalAs(UnmanagedType.Bool)] out bool exists);
        void GetLocaleNameLength(uint index, out uint length);
        void GetLocaleName(uint index, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, uint size);
        void GetStringLength(uint index, out uint length);
        void GetString(uint index, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, uint size);
    }
}
