using System.Globalization;
using System.Text;

namespace HoldItTogether.Services.Themes;

public sealed partial record HitAppearancePreferences
{
    // Null identifies legacy snapshots. Empty explicitly inherits every theme property.
    public string? EditedProperties { get; init; }
    public HitSphereStateAppearance? CollapsedSphere { get; init; }
    public HitSphereStateAppearance? InactiveSphere { get; init; }

    private static readonly (string Name, Func<HitAppearancePreferences, object?> Read)[] EditableProperties =
    [
        (nameof(Background), p => p.Background), (nameof(IdleBackground), p => p.IdleBackground),
        (nameof(Accent), p => p.Accent), (nameof(Text), p => p.Text), (nameof(Muted), p => p.Muted),
        (nameof(Border), p => p.Border), (nameof(Eyes), p => p.Eyes), (nameof(ShadowColor), p => p.ShadowColor),
        (nameof(Radius), p => p.Radius), (nameof(BorderWidth), p => p.BorderWidth), (nameof(BorderStyle), p => p.BorderStyle),
        (nameof(ControlRadius), p => p.ControlRadius), (nameof(AllowArtworkColors), p => p.AllowArtworkColors),
        (nameof(ShadowStrength), p => p.ShadowStrength), (nameof(ShadowBlur), p => p.ShadowBlur), (nameof(ShadowOffset), p => p.ShadowOffset),
        (nameof(Font), p => p.Font), (nameof(CustomFont), p => p.CustomFont),
        (nameof(TextSize), p => p.TextSize), (nameof(TextLineHeight), p => p.TextLineHeight),
        (nameof(TitleSize), p => p.TitleSize), (nameof(TitleWeight), p => p.TitleWeight), (nameof(TitleLetterSpacing), p => p.TitleLetterSpacing),
        (nameof(SphereSize), p => p.SphereSize), (nameof(InactiveSphereSize), p => p.InactiveSphereSize),
        (nameof(CollapsedSphere), p => p.CollapsedSphere), (nameof(InactiveSphere), p => p.InactiveSphere),
        (nameof(EyeSize), p => p.EyeSize), (nameof(EyeHeight), p => p.EyeHeight), (nameof(EyeGap), p => p.EyeGap),
        (nameof(EyeRoundness), p => p.EyeRoundness), (nameof(EyeOffsetX), p => p.EyeOffsetX), (nameof(EyeOffsetY), p => p.EyeOffsetY),
        (nameof(TrackPointer), p => p.TrackPointer),
        (nameof(BackgroundMedia), p => string.Join(',', p.MediaIds)),
        (nameof(BackgroundMode), p => p.BackgroundMode), (nameof(BackgroundIntervalSeconds), p => p.BackgroundIntervalSeconds),
        (nameof(BackgroundTransitionMs), p => p.BackgroundTransitionMs), (nameof(BackgroundTransition), p => p.BackgroundTransition),
        (nameof(BackgroundWhenCollapsed), p => p.BackgroundWhenCollapsed), (nameof(CollapsedBackgroundOpacity), p => p.CollapsedBackgroundOpacity),
        (nameof(ImageFit), p => p.ImageFit), (nameof(ImageOpacity), p => p.ImageOpacity),
        (nameof(BackgroundPositionX), p => p.BackgroundPositionX), (nameof(BackgroundPositionY), p => p.BackgroundPositionY),
        (nameof(BackgroundBlur), p => p.BackgroundBlur), (nameof(BackgroundSaturation), p => p.BackgroundSaturation),
        (nameof(BackgroundBrightness), p => p.BackgroundBrightness), (nameof(BackgroundContrast), p => p.BackgroundContrast),
        (nameof(BackgroundGrayscale), p => p.BackgroundGrayscale), (nameof(BackgroundZoom), p => p.BackgroundZoom), (nameof(BackgroundRotation), p => p.BackgroundRotation)
    ];
    private static readonly HashSet<string> EditableNames = EditableProperties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    private static readonly HitAppearancePreferences EditorDefaults = new();

    private static string? NormalizeEditedProperties(string? properties) => properties is null ? null
        : string.Join(',', properties.Split(',').Where(EditableNames.Contains).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

    // Older builds used System as the default font before theme inheritance was available.
    private HashSet<string> Overrides() => EditedProperties is not null
        ? EditedProperties.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(EditableNames.Contains).ToHashSet(StringComparer.Ordinal)
        : EditableProperties.Where(property => !(property.Name == nameof(Font) && Font == "system") && !Equals(property.Read(this), property.Read(EditorDefaults))).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

    public HitAppearancePreferences WithEditsFrom(HitAppearancePreferences previous, params string[] editedProperties)
    {
        var current = Normalize();
        var edits = previous.Normalize().Overrides();
        foreach (var property in EditableProperties)
            if (!Equals(property.Read(current), property.Read(previous))) edits.Add(property.Name);
        edits.UnionWith(editedProperties.Where(EditableNames.Contains));
        return current with { EditedProperties = string.Join(',', edits.Order(StringComparer.Ordinal)) };
    }

    public HitAppearancePreferences RestoreSelectFrom(HitAppearancePreferences original, string property)
    {
        var restored = property switch
        {
            nameof(Font) => this with { Font = original.Font },
            nameof(BorderStyle) => this with { BorderStyle = original.BorderStyle },
            nameof(BackgroundMode) => this with { BackgroundMode = original.BackgroundMode },
            nameof(BackgroundTransition) => this with { BackgroundTransition = original.BackgroundTransition },
            nameof(ImageFit) => this with { ImageFit = original.ImageFit },
            nameof(CollapsedSphere) => this with { CollapsedSphere = original.CollapsedSphere },
            nameof(InactiveSphere) => this with { InactiveSphere = original.InactiveSphere },
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };
        var edits = Overrides();
        var previousEdits = original.Overrides();
        if (previousEdits.Contains(property)) edits.Add(property); else edits.Remove(property);
        var unchanged = EditableProperties.All(candidate => Equals(candidate.Read(restored), candidate.Read(original)));
        return restored with
        {
            EditedProperties = unchanged && edits.SetEquals(previousEdits) ? original.EditedProperties : string.Join(',', edits.Order(StringComparer.Ordinal)),
            Enabled = unchanged ? original.Enabled : Enabled
        };
    }

    public HitAppearancePreferences RestoreSphereDefaults()
    {
        var defaults = EditorDefaults;
        var restored = this with
        {
            SphereSize = defaults.SphereSize, InactiveSphereSize = defaults.InactiveSphereSize,
            CollapsedSphere = null, InactiveSphere = null,
            Eyes = defaults.Eyes, EyeSize = defaults.EyeSize, EyeHeight = defaults.EyeHeight, EyeGap = defaults.EyeGap,
            EyeRoundness = defaults.EyeRoundness, EyeOffsetX = defaults.EyeOffsetX, EyeOffsetY = defaults.EyeOffsetY, TrackPointer = defaults.TrackPointer,
            IdleBackground = defaults.IdleBackground, BackgroundWhenCollapsed = defaults.BackgroundWhenCollapsed, CollapsedBackgroundOpacity = defaults.CollapsedBackgroundOpacity
        };
        var edits = Overrides();
        edits.ExceptWith([nameof(CollapsedSphere), nameof(InactiveSphere), nameof(SphereSize), nameof(InactiveSphereSize), nameof(Eyes), nameof(EyeSize), nameof(EyeHeight), nameof(EyeGap), nameof(EyeRoundness), nameof(EyeOffsetX), nameof(EyeOffsetY), nameof(TrackPointer),
            nameof(IdleBackground), nameof(BackgroundWhenCollapsed), nameof(CollapsedBackgroundOpacity)]);
        return (restored with { EditedProperties = string.Join(',', edits.Order(StringComparer.Ordinal)) }).Normalize();
    }

    public HitAppearancePreferences RestoreSphereStateDefaults(bool inactive)
    {
        var edits=Overrides();edits.Remove(inactive?nameof(InactiveSphere):nameof(CollapsedSphere));
        return (inactive?this with{InactiveSphere=new()}:this with{CollapsedSphere=new()}) with{EditedProperties=string.Join(',',edits.Order(StringComparer.Ordinal))};
    }

    public HitSphereStateAppearance SphereState(bool inactive)
    {
        var p=Normalize();
        if((inactive?p.InactiveSphere:p.CollapsedSphere) is {} state)return state;
        var edited=p.Overrides();bool Has(string key)=>edited.Contains(key);
        var size=inactive?Has(nameof(InactiveSphereSize))?p.InactiveSphereSize:null:Has(nameof(SphereSize))?p.SphereSize:null;
        return new()
        {
            Size=size,Roundness=size is null?null:50,
            Background=Has(nameof(IdleBackground))?p.IdleBackground??p.Background:null,
            EyeColor=Has(nameof(Eyes))?p.Eyes??p.Text:null,EyeSize=Has(nameof(EyeSize))?p.EyeSize:null,
            EyeHeight=Has(nameof(EyeHeight))?p.EyeHeight:null,EyeGap=Has(nameof(EyeGap))?p.EyeGap:null,
            EyeRoundness=Has(nameof(EyeRoundness))?p.EyeRoundness:null,EyeOffsetX=Has(nameof(EyeOffsetX))?p.EyeOffsetX:null,
            EyeOffsetY=Has(nameof(EyeOffsetY))?p.EyeOffsetY:null,TrackPointer=Has(nameof(TrackPointer))?p.TrackPointer:null,
            ShowBackground=Has(nameof(BackgroundWhenCollapsed))?p.BackgroundWhenCollapsed:null,
            BackgroundOpacity=Has(nameof(CollapsedBackgroundOpacity))?p.CollapsedBackgroundOpacity:null
        };
    }

    public string Css(string? imageUrl = null)
    {
        var p = Normalize();
        if (!p.Enabled) return "";
        var edited = p.Overrides();
        bool Has(string property) => edited.Contains(property);
        static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);
        var variables = new StringBuilder();
        var rules = new StringBuilder();
        void Rule(string selector, string declarations) { if (declarations.Length > 0) rules.Append(selector).Append('{').Append(declarations).Append('}'); }
        void Variable(string property, string declarations) { if (Has(property)) variables.Append(declarations); }
        var paletteSelector = p.AllowArtworkColors ? ".dynamic-sphere:not([data-audio-theme])" : ".dynamic-sphere";

        Variable(nameof(Background), $"--hit-background:{p.Background};--hit-open-background:{p.Background};");
        Variable(nameof(Text), $"--hit-text:{p.Text};--sphere-text:{p.Text};--hit-menu-text:{p.Text};");
        Variable(nameof(Muted), $"--hit-muted:{p.Muted ?? p.Text};--hit-menu-muted:{p.Muted ?? p.Text};");
        Variable(nameof(Accent), $"--hit-accent:{p.Accent};");
        Variable(nameof(Radius), $"--hit-open-radius:{p.Radius}px;");
        if (Has(nameof(Background))) Rule(paletteSelector, $"background-color:{p.Background}!important;");

        var open = new StringBuilder();
        if (Has(nameof(Radius))) open.Append($"border-radius:{p.Radius}px!important;");
        if (Has(nameof(Border))) open.Append($"outline-color:{p.Border};");
        if (Has(nameof(BorderWidth))) open.Append($"outline-width:{p.BorderWidth}px;outline-offset:-{p.BorderWidth}px;");
        if (Has(nameof(BorderStyle))) open.Append($"outline-style:{p.BorderStyle};");
        if (Has(nameof(ShadowStrength)) || Has(nameof(ShadowBlur)) || Has(nameof(ShadowOffset)) || Has(nameof(ShadowColor)))
            open.Append($"box-shadow:0 {p.ShadowOffset}px {p.ShadowBlur}px -8px color-mix(in srgb,{p.ShadowColor} {Number(p.ShadowStrength * .4)}%,transparent)!important;");
        Rule(".dynamic-sphere.is-open", open.ToString());

        if (Has(nameof(ControlRadius))) Rule(".hit-menu-panel :is(button:not([role=option]),select,input[type=text],input[type=number])", $"border-radius:{p.ControlRadius}px!important;");
        if ((Has(nameof(Font)) || Has(nameof(CustomFont))) && p.FontFamily is { } font)
        {
            variables.Append($"--hit-font:{font};--hit-menu-font:{font};");
            Rule(".dynamic-sphere,.hit-menu-panel", $"font-family:{font}!important;");
        }
        const string textSelector = ".hit-menu-panel :is(button,input,select,textarea,.hit-setting-row,.setting-row,.settings-slider-label,.hit-muted)";
        if (Has(nameof(TextSize)) && p.TextSize is { } textSize) Rule(textSelector, $"font-size:{textSize}px!important;");
        if (Has(nameof(TextLineHeight)) && p.TextLineHeight is { } lineHeight) Rule(textSelector, $"line-height:{Number(lineHeight)}!important;");
        var title = new StringBuilder();
        if (Has(nameof(TitleSize))) title.Append($"font-size:{p.TitleSize}px!important;");
        if (Has(nameof(TitleWeight))) title.Append($"font-weight:{p.TitleWeight}!important;");
        if (Has(nameof(TitleLetterSpacing))) title.Append($"letter-spacing:{Number(p.TitleLetterSpacing)}px!important;");
        Rule(".dynamic-sphere .sphere-title", title.ToString());

        if (!p.AllowArtworkColors)
        {
            var music = new StringBuilder();
            if (Has(nameof(Background))) music.Append($"--audio-art-background:{p.Background}!important;--audio-art-surface:{p.Background}!important;");
            if (Has(nameof(Text))) music.Append($"--audio-art-foreground:{p.Text}!important;");
            if (Has(nameof(Muted))) music.Append($"--audio-art-muted:{p.Muted ?? p.Text}!important;");
            if (Has(nameof(Accent))) music.Append($"--audio-art-accent:{p.Accent}!important;");
            Rule(".dynamic-sphere,.audio-menu[data-audio-palette]", music.ToString());
        }

        const string backdrop = ".dynamic-sphere > .sphere-custom-backdrop";
        var media = new StringBuilder();
        if (imageUrl is not null) media.Append($"display:block;background-image:url(\"{imageUrl}\");");
        if (imageUrl is not null || Has(nameof(ImageFit))) media.Append($"background-size:{(p.ImageFit == "stretch" ? "100% 100%" : p.ImageFit)};");
        if (imageUrl is not null || Has(nameof(ImageOpacity))) media.Append($"opacity:{Number(p.ImageOpacity / 100d)};");
        var filters = new List<string>();
        if (Has(nameof(BackgroundBlur))) filters.Add($"blur({p.BackgroundBlur}px)");
        if (Has(nameof(BackgroundSaturation))) filters.Add($"saturate({p.BackgroundSaturation}%)");
        if (Has(nameof(BackgroundBrightness))) filters.Add($"brightness({p.BackgroundBrightness}%)");
        if (Has(nameof(BackgroundContrast))) filters.Add($"contrast({p.BackgroundContrast}%)");
        if (Has(nameof(BackgroundGrayscale))) filters.Add($"grayscale({p.BackgroundGrayscale}%)");
        if (filters.Count > 0) media.Append($"filter:{string.Join(' ', filters)};");
        var transforms = new List<string>();
        if (Has(nameof(BackgroundZoom))) transforms.Add($"scale({Number(p.BackgroundZoom / 100d)})");
        if (Has(nameof(BackgroundRotation))) transforms.Add($"rotate({p.BackgroundRotation}deg)");
        if (transforms.Count > 0) media.Append($"transform:{string.Join(' ', transforms)};");
        Rule(backdrop, media.ToString());
        if (Has(nameof(BackgroundPositionX)) || Has(nameof(BackgroundPositionY)))
        {
            Rule(backdrop + "," + backdrop + "::after," + backdrop + " .custom-background-frame", $"background-position:{p.BackgroundPositionX}% {p.BackgroundPositionY}%;");
            Rule(backdrop + " video", $"object-position:{p.BackgroundPositionX}% {p.BackgroundPositionY}%;");
        }

        rules.Append(p.SphereState(false).Css(p,false));
        rules.Append(p.SphereState(true).Css(p,true));
        if (variables.Length == 0 && rules.Length == 0) return "";
        return $"@layer sphere {{:root {{{variables}}}{rules}}}\n@layer menus {{:root {{{variables}}}}}";
    }
}
