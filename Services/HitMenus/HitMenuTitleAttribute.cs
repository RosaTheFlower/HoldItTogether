namespace HoldItTogether.Services.HitMenus;

/// <summary>Optional menu title. The file name supplies a title when this attribute is omitted.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HitMenuTitleAttribute(string title) : Attribute
{
    public string Title { get; } = title;
}