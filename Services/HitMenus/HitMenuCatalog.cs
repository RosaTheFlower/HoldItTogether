using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace HoldItTogether.Services.HitMenus;

public sealed record HitMenuDefinition(string Id, string Name, Type ComponentType);

public sealed class HitMenuCatalog
{
    public IReadOnlyList<HitMenuDefinition> Menus { get; } =
        Discover(typeof(HitMenuCatalog).Assembly);

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
            .OrderBy(type => type.FullName == menuNamespace + ".HitMenuConfiguracoes" ? 1 : 0)
            .ThenBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => new HitMenuDefinition(
                type.FullName!,
                !string.IsNullOrWhiteSpace(type.GetCustomAttribute<HitMenuTitleAttribute>()?.Title)
                    ? type.GetCustomAttribute<HitMenuTitleAttribute>()!.Title.Trim()
                    : type.FullName == menuNamespace + ".HitMenuConfiguracoes"
                    ? "Configurações"
                    : Regex.Replace(type.Name[prefix.Length..], @"([a-z0-9])([A-Z])", "$1 $2"),
                type))
            .ToArray());
    }
}
