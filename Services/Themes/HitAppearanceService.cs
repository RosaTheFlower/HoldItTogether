using System.Text.Json;
using System.Text.RegularExpressions;
using HoldItTogether.Services.Settings;

namespace HoldItTogether.Services.Themes;

public sealed record HitAppearancePreferences
{
    public bool Enabled {get;init;}
    public string Background {get;init;}="#17191e";
    public string Accent {get;init;}="#dce2ec";
    public string Text {get;init;}="#f1f2f4";
    public string Border {get;init;}="#44474f";
    public int Radius {get;init;}=22;
    public int BorderWidth {get;init;}=1;
    public bool AllowArtworkColors {get;init;}=true;
    public string? IdleBackground {get;init;}
    public string? Muted {get;init;}
    public string? Eyes {get;init;}
    public int EyeSize {get;init;}=8;
    public int EyeGap {get;init;}=8;
    public bool TrackPointer {get;init;}=true;
    public int ControlRadius {get;init;}=8;
    public int ShadowStrength {get;init;}=100;
    public string BorderStyle {get;init;}="solid";
    public string Font {get;init;}="system";
    public string? BackgroundImage {get;init;}
    public string BackgroundImages {get;init;}="";
    public string BackgroundMode {get;init;}="single";
    public int BackgroundIntervalSeconds {get;init;}=15;
    public int BackgroundTransitionMs {get;init;}=1500;
    public string BackgroundTransition {get;init;}="fade";
    public bool BackgroundWhenCollapsed {get;init;}=true;
    public double? BackgroundImageLimitMb {get;init;}=2;
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] ImageIds => new[]{BackgroundImage}.Concat((BackgroundImages??"").Split(',',StringSplitOptions.RemoveEmptyEntries)).Where(HitAppearanceService.ValidImageId).Select(id=>id!).Distinct(StringComparer.Ordinal).Take(8).ToArray();
    public int ImageOpacity {get;init;}=35;
    public string ImageFit {get;init;}="cover";
    private static string Color(string? value,string fallback)=>value is not null&&Regex.IsMatch(value,"^#[0-9a-fA-F]{6}$",RegexOptions.CultureInvariant)?value.ToLowerInvariant():fallback;
    private static string? OptionalColor(string? value)=>value is null?null:Color(value,"#17191e");
    public HitAppearancePreferences Normalize()=>this with{Background=Color(Background,"#17191e"),Accent=Color(Accent,"#dce2ec"),Text=Color(Text,"#f1f2f4"),Border=Color(Border,"#44474f"),Radius=Math.Clamp(Radius,0,48),BorderWidth=Math.Clamp(BorderWidth,0,4),
        IdleBackground=OptionalColor(IdleBackground),Muted=OptionalColor(Muted),Eyes=OptionalColor(Eyes),EyeSize=Math.Clamp(EyeSize,3,16),EyeGap=Math.Clamp(EyeGap,0,20),ControlRadius=Math.Clamp(ControlRadius,0,24),ShadowStrength=Math.Clamp(ShadowStrength,0,150),
        BorderStyle=BorderStyle is "dashed" or "dotted"?BorderStyle:"solid",Font=Font is "mono" or "arial"?Font:"system",BackgroundImage=HitAppearanceService.ValidImageId(BackgroundImage)?BackgroundImage:null,ImageOpacity=Math.Clamp(ImageOpacity,0,100),ImageFit=ImageFit is "contain" or "stretch"?ImageFit:"cover",
        BackgroundImages=string.Join(",",ImageIds.Where(id=>id!=BackgroundImage)),BackgroundMode=BackgroundMode is "carousel" or "smooth"?BackgroundMode:"single",BackgroundIntervalSeconds=Math.Clamp(BackgroundIntervalSeconds,5,3600),BackgroundTransitionMs=Math.Clamp(BackgroundTransitionMs,100,4000),BackgroundTransition=BackgroundTransition is "slide" or "zoom"?BackgroundTransition:"fade",
        BackgroundImageLimitMb=BackgroundImageLimitMb is null?null:double.IsFinite(BackgroundImageLimitMb.Value)?Math.Max(.25,BackgroundImageLimitMb.Value):2};
    public string Css(string? imageUrl=null)
    {
        var p=Normalize();if(!p.Enabled)return "";
        var muted=p.Muted??$"color-mix(in srgb,{p.Text} 65%,{p.Background})";
        var font=p.Font switch{"mono"=>"Consolas,monospace","arial"=>"Arial,sans-serif",_=>"'Segoe UI Variable','Segoe UI',sans-serif"};
        var colors=$"--hit-background:{p.Background};--hit-open-background:{p.Background};--hit-idle-background:{p.IdleBackground??p.Background};--hit-text:{p.Text};--hit-muted:{muted};--hit-accent:{p.Accent};--sphere-text:{p.Text};--hit-menu-text:{p.Text};--hit-menu-muted:{muted};--hit-font:{font};--hit-menu-font:{font};";
        var selector=p.AllowArtworkColors?".dynamic-sphere:not([data-audio-theme])":".dynamic-sphere";
        var music=p.AllowArtworkColors?"":$".dynamic-sphere,.audio-menu[data-audio-palette]{{--audio-art-background:{p.Background}!important;--audio-art-surface:{p.Background}!important;--audio-art-foreground:{p.Text}!important;--audio-art-muted:color-mix(in srgb,{p.Text} 65%,{p.Background})!important;--audio-art-accent:{p.Accent}!important;}}";
        var extras=$"{selector}:not(.is-open){{background-color:{p.IdleBackground??p.Background}!important;}}.dynamic-sphere{{ --hit-track-pointer:{(p.TrackPointer?1:0)};--hit-eye-size:{p.EyeSize}px!important;--hit-eye-gap:{p.EyeGap}px!important;}}.dynamic-sphere:not(.is-open)::before,.dynamic-sphere:not(.is-open)::after{{background-color:{p.Eyes??p.Text}!important;}}.dynamic-sphere.is-open{{box-shadow:0 10px 24px -8px rgb(0 0 0 / {(p.ShadowStrength*.004).ToString(System.Globalization.CultureInfo.InvariantCulture)})!important;}}.hit-menu-panel :is(button:not([role=option]),select,input[type=text],input[type=number]){{border-radius:{p.ControlRadius}px!important;}}";
        var imageSelector=".dynamic-sphere";
        extras+=$".dynamic-sphere,.hit-menu-panel{{font-family:{font}!important;}}";
        var image=imageUrl is null?"":$"{imageSelector} > .sphere-custom-backdrop{{display:block;background-image:url(\"{imageUrl}\");background-size:{(p.ImageFit=="stretch"?"100% 100%":p.ImageFit)};opacity:{(p.ImageOpacity/100d).ToString(System.Globalization.CultureInfo.InvariantCulture)};}}";
        if (!p.BackgroundWhenCollapsed) image += ".dynamic-sphere:not(.is-open) > .sphere-custom-backdrop{display:none!important;}";
        return "@layer sphere {:root {"+colors+"--hit-open-radius:"+p.Radius+"px;}"+selector+"{background-color:"+p.Background+"!important;}.dynamic-sphere.is-open{border-radius:"+p.Radius+"px!important;outline:"+p.BorderWidth+"px "+p.BorderStyle+" "+p.Border+";outline-offset:-"+p.BorderWidth+"px;}"+extras+music+image+"}\n@layer menus {:root {"+colors+"}}";
    }
}
public sealed record HitVisualProfile(string Id,string Name,string Theme,HitAppearancePreferences Appearance);

public sealed class HitAppearanceService
{
    public const int MaxImageBytes=2*1024*1024;
    public string ImageDirectory {get;}
    public static bool ValidImageId(string? id)=>id is not null&&Regex.IsMatch(id,"^[a-f0-9]{32}\\.(png|jpg|webp|gif)$",RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    private readonly HitSettingsService _settings;
    private readonly HitThemeService _themes;
    private readonly string _directory;
    private HitAppearancePreferences? _preview;
    public string? PreviewTheme {get;private set;}
    public HitAppearancePreferences Current=>_preview??_settings.Current.Appearance;
    public bool GifBackgroundsAllowed => _settings.Current.AllowGifBackgrounds;
    public long ImageByteLimit => Current.Normalize().BackgroundImageLimitMb is { } mb && mb < long.MaxValue / 1_000_000d ? (long)Math.Ceiling(mb * 1_000_000) : long.MaxValue;
    public event Action? Changed;
    public HitAppearanceService(HitSettingsService settings,HitThemeService themes):this(settings,themes,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HoldItTogether","Appearance")){}
    public HitAppearanceService(HitSettingsService settings,HitThemeService themes,string directory){_settings=settings;_themes=themes;_directory=Path.GetFullPath(directory);ImageDirectory=Path.Combine(_directory,"Images");Directory.CreateDirectory(ImageDirectory);}
    public async Task<string> ImportImageAsync(Stream source,CancellationToken token=default)
    {
        var limit = ImageByteLimit;
        var bytes = new byte[12];
        var length = await source.ReadAtLeastAsync(bytes,12,false,token);
        var extension=bytes.Length>=6&&(System.Text.Encoding.ASCII.GetString(bytes,0,6) is "GIF87a" or "GIF89a")?"gif":bytes.Length>=8&&bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})?"png":bytes.Length>=3&&bytes[0]==255&&bytes[1]==216&&bytes[2]==255?"jpg":bytes.Length>=12&&System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(bytes,8,4)=="WEBP"?"webp":throw new InvalidDataException("Choose a PNG, JPEG, WebP or GIF image.");
        if (length < 12) throw new InvalidDataException("Choose a valid background image.");
        if(extension=="gif"&&!GifBackgroundsAllowed)throw new InvalidDataException("GIF backgrounds are disabled in Settings > Images.");
        var id=Guid.NewGuid().ToString("N")+"."+extension;
        var destination=Path.Combine(ImageDirectory,id); var temporary=destination+".tmp";
        try
        {
            await using (var output = File.Create(temporary))
            {
                await output.WriteAsync(bytes.AsMemory(0,length),token);
                long total=length;var block=new byte[81920];int count;
                while ((count=await source.ReadAsync(block,token))>0)
                {
                    if (count > limit-total) throw new InvalidDataException("The image exceeds the configured size limit.");
                    await output.WriteAsync(block.AsMemory(0,count),token);total+=count;
                }
            }
            token.ThrowIfCancellationRequested();File.Move(temporary,destination);return id;
        }
        finally { if(File.Exists(temporary))File.Delete(temporary); }
    }
    public string? ImageUrl(string? id)=>ValidImageId(id)&&(GifBackgroundsAllowed||!id!.EndsWith(".gif",StringComparison.Ordinal))&&File.Exists(Path.Combine(ImageDirectory,id!))?"appearance-images/"+id:null;
    public void RemoveImportedImage(string id){if(!ValidImageId(id))throw new ArgumentException("Invalid image ID.");File.Delete(Path.Combine(ImageDirectory,id));}
    public string Css(HitAppearancePreferences appearance,bool embedImage=false)
    {
        var p=appearance.Normalize();if(!p.Enabled)return "";var selected=p.ImageIds.FirstOrDefault(id=>ImageUrl(id)is not null);var url=ImageUrl(selected);
        if(url is not null&&embedImage){var path=Path.Combine(ImageDirectory,selected!);if(new FileInfo(path).Length>HitThemeService.MaxCssBytes)throw new InvalidDataException("The exported theme exceeds 1 MB. Use smaller background images.");var mime=Path.GetExtension(path) switch{".jpg"=>"image/jpeg",".webp"=>"image/webp",".gif"=>"image/gif",_=>"image/png"};url="data:"+mime+";base64,"+Convert.ToBase64String(File.ReadAllBytes(path));}
        if (!embedImage || p.BackgroundMode == "single" || p.ImageIds.Length < 2) return p.Css(url);
        var urls = p.ImageIds.Where(id=>ImageUrl(id)is not null).Select(id =>
        {
            var path = Path.Combine(ImageDirectory,id);
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > HitThemeService.MaxCssBytes) throw new InvalidDataException("The exported theme exceeds 1 MB. Use smaller background images.");
            var mime = Path.GetExtension(path) switch { ".jpg" => "image/jpeg", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/png" };
            return "data:"+mime+";base64,"+Convert.ToBase64String(File.ReadAllBytes(path));
        }).Where(value => value is not null).Select(value => value!).ToArray();
        return p.Css(url) + ExportSlideshow(p, urls);
    }
    private static string ExportSlideshow(HitAppearancePreferences p, string[] urls)
    {
        if (urls.Length < 2) return "";
        static string N(double number) => number.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);
        var current = new System.Text.StringBuilder(); var next = new System.Text.StringBuilder(); var fade = new System.Text.StringBuilder();
        for (var i = 0; i <= urls.Length; i++)
        {
            var percent = 100d * i / urls.Length;
            current.Append($"{N(percent)}%{{background-image:url(\"{urls[i % urls.Length]}\");}}");
            next.Append($"{N(percent)}%{{background-image:url(\"{urls[(i + 1) % urls.Length]}\");}}");
            if (i == urls.Length) { fade.Append("100%{opacity:0;transform:none;}"); break; }
            var hold = percent + (100d / urls.Length) * (1 - p.BackgroundTransitionMs / (p.BackgroundIntervalSeconds * 1000d));
            var transform = p.BackgroundTransition switch { "slide" => "translateX(12%)", "zoom" => "scale(1.12)", _ => "none" };
            fade.Append($"{N(percent)}%,{N(hold)}%{{opacity:0;transform:{transform};}}{N(100d*(i+1)/urls.Length - .00001)}%{{opacity:1;transform:none;}}");
        }
        var selector = ".dynamic-sphere > .sphere-custom-backdrop";
        var duration = p.BackgroundIntervalSeconds * urls.Length;
        var css = $"@layer sphere {{@keyframes hit-export-background{{{current}}}@keyframes hit-export-next{{{next}}}@keyframes hit-export-fade{{{fade}}}";
        css += $"{selector}{{overflow:hidden;animation:hit-export-background {duration}s step-end infinite;}}";
        if (p.BackgroundMode == "smooth") css += $"{selector}::after{{content:'';position:absolute;inset:0;background-position:center;background-repeat:no-repeat;background-size:{(p.ImageFit=="stretch"?"100% 100%":p.ImageFit)};animation:hit-export-next {duration}s step-end infinite,hit-export-fade {duration}s linear infinite;}}";
        css += ".dynamic-sphere:not(.is-open) > .sphere-custom-backdrop,.dynamic-sphere:not(.is-open) > .sphere-custom-backdrop::after{animation-play-state:paused;}";
        return css + "@media(prefers-reduced-motion:reduce){.sphere-custom-backdrop::after{display:none!important;}}}";
    }
    public string ValidTheme(string theme)=>_themes.GetThemes().Any(item=>item.Id==theme)?theme:"default";
    public void Preview(HitAppearancePreferences appearance,string theme){_preview=appearance.Normalize();PreviewTheme=ValidTheme(theme);Changed?.Invoke();}
    public void Cancel(){_preview=null;PreviewTheme=null;Changed?.Invoke();}
    public void Apply(HitAppearancePreferences appearance,string theme){_settings.Update(value=>value with{Appearance=appearance.Normalize(),Theme=ValidTheme(theme)});Cancel();}
    public HitVisualProfile[] Profiles()
    {
        var path=Path.Combine(_directory,"profiles.json");
        if(!File.Exists(path))return [];
        if(new FileInfo(path).Length>128*1024)throw new InvalidDataException("The profile file is too large.");
        var profiles=JsonSerializer.Deserialize<HitVisualProfile[]>(File.ReadAllText(path),Json)??[];
        if(profiles.Length>50||profiles.Any(p=>p is null||!Guid.TryParse(p.Id,out _)||string.IsNullOrWhiteSpace(p.Name)||p.Name.Length>64||p.Appearance is null))throw new InvalidDataException("Invalid visual profiles.");
        return profiles.Select(p=>p with{Appearance=p.Appearance.Normalize()}).ToArray();
    }
    public HitVisualProfile Save(string name,HitAppearancePreferences appearance,string theme)
    {
        name=name.Trim();if(name.Length is <1 or >64||name.Any(char.IsControl))throw new ArgumentException("Use a profile name with 1-64 characters.");
        var profiles=Profiles();if(profiles.Length>=50)throw new InvalidOperationException("You can save up to 50 visual profiles.");
        var profile=new HitVisualProfile(Guid.NewGuid().ToString("N"),name,ValidTheme(theme),appearance.Normalize());
        Directory.CreateDirectory(_directory);var path=Path.Combine(_directory,"profiles.json");
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(profiles.Append(profile),Json));File.Move(path+".tmp",path,true);return profile;
    }
    public string Export(HitAppearancePreferences appearance,string theme)
    {
        var css=_themes.ReadCurrent(ValidTheme(theme)).Css+"\n"+Css(appearance,true);
        if(System.Text.Encoding.UTF8.GetByteCount(css)>HitThemeService.MaxCssBytes)throw new InvalidOperationException("The exported theme exceeds 1 MB.");
        var directory=Path.Combine(_directory,"Exports");Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,$"HIT-Custom-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.css");
        File.WriteAllText(path,css);return path;
    }
}
