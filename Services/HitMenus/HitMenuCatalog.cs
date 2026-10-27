using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using HoldItTogether.Services.Extensions;

namespace HoldItTogether.Services.HitMenus;

public sealed record HitMenuDefinition(string Id, string Name, Type? ComponentType)
{
    public string? ExtensionId { get; init; }
    public ExtensionApi? Api { get; init; }
    public string[] Styles { get; init; } = [];
}

public sealed class HitMenuCatalog : IDisposable
{
    public const string SettingsId = "HoldItTogether.Components.HitMenus.HitMenuSettings";
    private readonly ExtensionStore? _store;
    private readonly ExtensionRuntime? _runtime;
    public event Action? Changed;
    public IReadOnlyList<HitMenuDefinition> Menus { get; private set; } = [];
    public IReadOnlyList<HitMenuDefinition> All { get; private set; } = [];
    public HitMenuCatalog() => Refresh();
    public HitMenuCatalog(ExtensionStore store, ExtensionRuntime runtime)
    { _store = store; _runtime = runtime; store.Changed += Refresh; runtime.Changed += Refresh; Refresh(); }
    public void Refresh()
    {
        var menus = Discover(typeof(HitMenuCatalog).Assembly).ToList();
        if (_store is not null)
            foreach (var package in _store.State.Packages.Where(package => !package.PendingDelete))
                foreach (var menu in package.Manifest.Menus)
                {
                    var id = package.Manifest.Id + ":" + menu.Id;
                    var loaded = _runtime?.Menus.FirstOrDefault(item => item.Id == id);
                    menus.Add(new(id, menu.Title, loaded?.ComponentType) { ExtensionId = package.Manifest.Id, Api = loaded?.Api, Styles = loaded?.Styles ?? [] });
                }
        var order = _store?.State.Order ?? [];
        All = menus.OrderBy(menu => menu.Id == SettingsId ? 1 : 0)
            .ThenBy(menu => { var index = Array.IndexOf(order, menu.Id); return index < 0 ? int.MaxValue : index; }).ToArray();
        Menus = All.Where(menu => menu.ComponentType is not null && (menu.Id == SettingsId || (_store?.IsEnabled(menu.Id) ?? true))).ToArray();
        Changed?.Invoke();
    }
    public void Move(string source, string target)
    {
        if (_store is null || source == SettingsId || target == SettingsId || source == target) return;
        var ids = All.Where(item => item.Id != SettingsId).Select(item => item.Id).ToList();
        var from = ids.IndexOf(source); var to = ids.IndexOf(target);
        if (from < 0 || to < 0) return;
        ids.RemoveAt(from); ids.Insert(to, source); _store.SetOrder(ids);
    }
    public void Place(string source, string target, bool after)
    {
        if (_store is null || source == SettingsId || target == SettingsId || source == target) return;
        var ids = All.Where(menu => menu.Id != SettingsId).Select(menu => menu.Id).ToList();
        if (!ids.Remove(source)) return;
        var index = ids.IndexOf(target);
        if (index < 0) return;
        ids.Insert(index + (after ? 1 : 0), source); _store.SetOrder(ids);
    }
    public void Dispose() { if (_store is not null) _store.Changed -= Refresh; if (_runtime is not null) _runtime.Changed -= Refresh; }

    public static IReadOnlyList<HitMenuDefinition> Discover(Assembly assembly)
    {
        const string menuNamespace = "HoldItTogether.Components.HitMenus";
        const string prefix = "HitMenu";

        return Array.AsReadOnly(assembly.GetTypes()
            .Where(type =>
                type.IsPublic && !type.IsAbstract && !type.ContainsGenericParameters &&
                typeof(IComponent).IsAssignableFrom(type) &&
                (type.Namespace == menuNamespace ||
                 type.Namespace?.StartsWith(menuNamespace + ".", StringComparison.Ordinal) == true) &&
                type.Name.StartsWith(prefix, StringComparison.Ordinal) &&
                type.Name.Length > prefix.Length)
            .OrderBy(type => type.FullName == menuNamespace + ".HitMenuSettings" ? 1 : 0)
            .ThenBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => new HitMenuDefinition(
                type.FullName!,
                !string.IsNullOrWhiteSpace(type.GetCustomAttribute<HitMenuTitleAttribute>()?.Title)
                    ? type.GetCustomAttribute<HitMenuTitleAttribute>()!.Title.Trim()
                    : type.FullName == menuNamespace + ".HitMenuSettings"
                    ? "Settings"
                    : Regex.Replace(type.Name[prefix.Length..], @"([a-z0-9])([A-Z])", "$1 $2"),
                type))
            .ToArray());
    }
}
