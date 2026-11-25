using System.Text;
using System.Text.Json;
using HoldItTogether.Services.Localization;
using HoldItTogether.Services.Settings;

internal static class LanguageChecks
{
    public static void Run()
    {
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static byte[] Pack(string locale = "es-ES", object? messages = null, object? version = null) => JsonSerializer.SerializeToUtf8Bytes(new
        { version = version ?? 1, locale, name = "Español", messages = messages ?? new Dictionary<string, string> { ["Settings"] = "Configuración" } });
        static void Reject(Action action)
        {
            try { action(); }
            catch (Exception error) when (error is InvalidDataException or IOException or JsonException or ArgumentException or FormatException) { return; }
            throw new Exception("Invalid language accepted");
        }
        var root = Path.Combine(Environment.CurrentDirectory, "obj", "language-tests", Guid.NewGuid().ToString("N"));
        var store = new HitLanguageStore(Path.Combine(root, "Languages"));
        var emptyData = new HitDataService(root); emptyData.Request(false); HitDataService.ApplyPending(root);
        Check(!Directory.Exists(Path.Combine(root,"Languages")), "Reset succeeds before any language has been installed or selected");
        Check(store.SelectedLocale == "en-US", "Default must be English");
        var builtIns = store.Installed();
        Check(builtIns.Count == 2 && builtIns[1].Messages["Settings"] == "Configurações", "Embedded Portuguese catalog");
        Check(HitText.Source.Keys.All(builtIns[1].Messages.ContainsKey), "Complete source catalog coverage");
        store.Import(Pack()); store.Select("es-ES");
        Check(new HitLanguageStore(Path.Combine(root, "Languages")).SelectedLocale == "es-ES", "Selection survives restart");
        Reject(() => store.Import(Pack("../bad")));
        Reject(() => store.Import(Pack("en-US")));
        Reject(() => store.Import(Pack("pt-BR")));
        Reject(() => store.Import(Pack(version: "1")));
        Reject(() => store.Import(Pack(messages: new Dictionary<string,string> { ["Unknown key"] = "Unknown" })));
        Reject(() => store.Import(Pack(messages: new Dictionary<string,string> { ["Open {0}"] = "Abrir {1}" })));
        Reject(() => store.Import(Pack(messages: new Dictionary<string,string> { ["Open {0}"] = "Abrir {0" })));
        Reject(() => store.Import(new byte[HitLanguageStore.MaximumBytes + 1]));
        Reject(() => store.Import(Pack(messages: new Dictionary<string,string> { ["Open {0}"] = "Abrir {0,999999999}" })));
        Reject(() => store.Import(Pack(messages: new Dictionary<string,string> { ["Open {0}"] = "Abrir {0:Q}" })));
        Reject(() => store.Import(Encoding.UTF8.GetBytes("{\"version\":1,\"locale\":\"es-ES\",\"name\":\"Test\",\"messages\":{\"Settings\":\"A\",\"Settings\":\"B\"}}")));
        Check(store.Installed().Single(item => item.Locale == "es-ES").Messages["Settings"] == "Configuración", "Rejected import preserves last valid catalog");
        File.WriteAllText(Path.Combine(root, "Languages", "broken.json"), "{");
        Check(store.Installed().Count == 3, "Corrupt file does not prevent startup");
        var data = new HitDataService(root); data.Request(false); HitDataService.ApplyPending(root);
        Check(store.SelectedLocale == "en-US" && store.Installed().Count == 3, "Reset selection but retain imported catalogs");
        data.Request(true); HitDataService.ApplyPending(root);
        Check(store.Installed().Count == 2, "Delete imports but preserve embedded languages");
        Console.WriteLine("Language checks passed.");
    }
}
