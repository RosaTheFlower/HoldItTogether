using System.Globalization;
using System.Text;

namespace HoldItTogether.Services.Themes;

// Null members inherit the theme/shared media settings. A null state snapshot loads legacy choices.
public sealed record HitSphereStateAppearance
{
    public int? Size { get; init; }
    public int? Roundness { get; init; }
    public int? Opacity { get; init; }
    public string? Background { get; init; }
    public int? BorderWidth { get; init; }
    public string? BorderStyle { get; init; }
    public string? BorderColor { get; init; }
    public string? ShadowColor { get; init; }
    public int? ShadowStrength { get; init; }
    public int? ShadowBlur { get; init; }
    public int? ShadowOffset { get; init; }
    public bool? ShowEyes { get; init; }
    public string? EyeColor { get; init; }
    public int? EyeSize { get; init; }
    public int? EyeHeight { get; init; }
    public int? EyeGap { get; init; }
    public int? EyeRoundness { get; init; }
    public int? EyeOffsetX { get; init; }
    public int? EyeOffsetY { get; init; }
    public int? EyeOpacity { get; init; }
    public int? EyeRotation { get; init; }
    public bool? TrackPointer { get; init; }
    public bool? ShowBackground { get; init; }
    public bool? PlayBackground { get; init; }
    public int? BackgroundOpacity { get; init; }
    public string? BackgroundFit { get; init; }
    public int? BackgroundPositionX { get; init; }
    public int? BackgroundPositionY { get; init; }
    public int? BackgroundZoom { get; init; }
    public int? BackgroundRotation { get; init; }
    public int? BackgroundBlur { get; init; }
    public int? BackgroundBrightness { get; init; }
    public int? BackgroundContrast { get; init; }
    public int? BackgroundSaturation { get; init; }
    public int? BackgroundGrayscale { get; init; }

    private static int? Limit(int? value,int min,int max)=>value is {} number?Math.Clamp(number,min,max):null;
    private static string? Color(string? value)=>HitColor.TryParse(value,out var color)?color.Hex:null;
    public HitSphereStateAppearance Normalize(bool inactive)=>this with
    {
        Size=Limit(Size,inactive?16:32,128),Roundness=Limit(Roundness,0,50),Opacity=Limit(Opacity,10,100),Background=Color(Background),
        BorderWidth=Limit(BorderWidth,0,4),BorderStyle=BorderStyle is "solid" or "dashed" or "dotted"?BorderStyle:null,BorderColor=Color(BorderColor),
        ShadowColor=Color(ShadowColor),ShadowStrength=Limit(ShadowStrength,0,150),ShadowBlur=Limit(ShadowBlur,0,60),ShadowOffset=Limit(ShadowOffset,0,30),
        EyeColor=Color(EyeColor),EyeSize=Limit(EyeSize,3,16),EyeHeight=Limit(EyeHeight,3,24),EyeGap=Limit(EyeGap,0,20),EyeRoundness=Limit(EyeRoundness,0,50),
        EyeOffsetX=Limit(EyeOffsetX,-16,16),EyeOffsetY=Limit(EyeOffsetY,-16,16),EyeOpacity=Limit(EyeOpacity,0,100),EyeRotation=Limit(EyeRotation,-180,180),
        BackgroundOpacity=Limit(BackgroundOpacity,0,100),BackgroundFit=BackgroundFit is "cover" or "contain" or "stretch"?BackgroundFit:null,
        BackgroundPositionX=Limit(BackgroundPositionX,0,100),BackgroundPositionY=Limit(BackgroundPositionY,0,100),BackgroundZoom=Limit(BackgroundZoom,100,200),
        BackgroundRotation=Limit(BackgroundRotation,-180,180),BackgroundBlur=Limit(BackgroundBlur,0,30),BackgroundBrightness=Limit(BackgroundBrightness,0,200),
        BackgroundContrast=Limit(BackgroundContrast,0,200),BackgroundSaturation=Limit(BackgroundSaturation,0,200),BackgroundGrayscale=Limit(BackgroundGrayscale,0,100)
    };

    internal string Css(HitAppearancePreferences shared,bool inactive)
    {
        var p=Normalize(inactive);
        var selector=inactive?".dynamic-sphere:not(.is-open).is-idle":".dynamic-sphere:not(.is-open):not(.is-idle)";
        var rules=new StringBuilder();var shape=new StringBuilder();var eyes=new StringBuilder();var media=new StringBuilder();
        static string N(double value)=>value.ToString(CultureInfo.InvariantCulture);
        void Rule(string target,string declarations){if(declarations.Length>0)rules.Append(target).Append('{').Append(declarations).Append('}');}
        if(p.Size is {} size)shape.Append($"width:{size}px!important;height:{size}px!important;");
        if(p.Roundness is {} roundness)shape.Append($"border-radius:{roundness}%!important;");
        if(p.Opacity is {} opacity)shape.Append($"opacity:{N(opacity/100d)}!important;");
        if(p.BorderWidth is {} width)shape.Append($"outline-width:{width}px;outline-offset:-{width}px;{(width>0&&p.BorderStyle is null?"outline-style:solid;":"")}");
        if(p.BorderStyle is {} style)shape.Append($"outline-style:{style};");
        if(p.BorderColor is {} border)shape.Append($"outline-color:{border};");
        if(p.ShadowColor is not null||p.ShadowStrength is not null||p.ShadowBlur is not null||p.ShadowOffset is not null)
            shape.Append($"box-shadow:0 {p.ShadowOffset??4}px {p.ShadowBlur??16}px -4px color-mix(in srgb,{p.ShadowColor??"#000000"} {N((p.ShadowStrength??100)*.4)}%,transparent)!important;");
        if(p.EyeSize is {} eyeSize)shape.Append($"--hit-eye-size:{eyeSize}px!important;");
        if(p.EyeGap is {} eyeGap)shape.Append($"--hit-eye-gap:{eyeGap}px!important;");
        if(p.TrackPointer is {} track)shape.Append($"--hit-track-pointer:{(track?1:0)};{(!track?"--hit-pointer-x:0!important;--hit-pointer-y:0!important;--hit-pointer-abs-x:0!important;--hit-pointer-abs-y:0!important;":"")}");
        Rule(selector,shape.ToString());
        if(p.Background is {} background)Rule(selector+(shared.AllowArtworkColors?":not([data-audio-theme])":""),$"background-color:{background}!important;");
        if(p.ShowEyes is {} show)eyes.Append($"display:{(show?"block":"none")}!important;");
        if(p.EyeColor is {} eyeColor)eyes.Append($"background-color:{eyeColor}!important;");
        if(p.EyeHeight is {} height)eyes.Append($"height:{height}px!important;");
        if(p.EyeRoundness is {} eyeRoundness)eyes.Append($"border-radius:{eyeRoundness}%!important;");
        if(p.EyeOffsetX is not null||p.EyeOffsetY is not null)eyes.Append($"translate:{p.EyeOffsetX??0}px {p.EyeOffsetY??0}px;");
        if(p.EyeOpacity is {} eyeOpacity)eyes.Append($"opacity:{N(eyeOpacity/100d)}!important;");
        if(p.EyeRotation is {} eyeRotation)eyes.Append($"rotate:{eyeRotation}deg;");
        Rule(selector+"::before,"+selector+"::after",eyes.ToString());

        var backdrop=selector+" > .sphere-custom-backdrop";
        if(p.ShowBackground is false)media.Append("display:none!important;");
        if(p.BackgroundOpacity is {} mediaOpacity)media.Append($"opacity:{N(mediaOpacity/100d)};");
        if(p.PlayBackground is {} play)media.Append($"--hit-background-play-state:{(play?"running":"paused")};");
        if(p.BackgroundBlur is not null||p.BackgroundBrightness is not null||p.BackgroundContrast is not null||p.BackgroundSaturation is not null||p.BackgroundGrayscale is not null)
            media.Append($"filter:blur({p.BackgroundBlur??shared.BackgroundBlur}px) saturate({p.BackgroundSaturation??shared.BackgroundSaturation}%) brightness({p.BackgroundBrightness??shared.BackgroundBrightness}%) contrast({p.BackgroundContrast??shared.BackgroundContrast}%) grayscale({p.BackgroundGrayscale??shared.BackgroundGrayscale}%);");
        if(p.BackgroundZoom is not null||p.BackgroundRotation is not null)
            media.Append($"transform:scale({N((p.BackgroundZoom??shared.BackgroundZoom)/100d)}) rotate({p.BackgroundRotation??shared.BackgroundRotation}deg);");
        Rule(backdrop,media.ToString());
        if(p.BackgroundFit is {} fit)
        {
            Rule(backdrop+","+backdrop+"::after,"+backdrop+" .custom-background-frame",$"background-size:{(fit=="stretch"?"100% 100%":fit)}!important;");
            Rule(backdrop+" video",$"object-fit:{(fit=="stretch"?"fill":fit)}!important;");
        }
        if(p.BackgroundPositionX is not null||p.BackgroundPositionY is not null)
        {
            var position=$"{p.BackgroundPositionX??shared.BackgroundPositionX}% {p.BackgroundPositionY??shared.BackgroundPositionY}%";
            Rule(backdrop+","+backdrop+"::after,"+backdrop+" .custom-background-frame",$"background-position:{position};");
            Rule(backdrop+" video",$"object-position:{position};");
        }
        return rules.ToString();
    }
}
