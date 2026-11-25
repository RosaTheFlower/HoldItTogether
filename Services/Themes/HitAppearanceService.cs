using Text = HoldItTogether.Services.Localization.HitText;
using System.Text.Json;
using System.Text.RegularExpressions;
using HoldItTogether.Services.Settings;

namespace HoldItTogether.Services.Themes;

public sealed partial record HitAppearancePreferences
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
    public string ShadowColor {get;init;}="#000000";
    public int EyeOffsetX {get;init;}
    public int EyeOffsetY {get;init;}
    public int? SphereSize {get;init;}
    public int? InactiveSphereSize {get;init;}
    public int EyeSize {get;init;}=8;
    public int EyeGap {get;init;}=8;
    public int? EyeHeight {get;init;}
    public int EyeRoundness {get;init;}=50;
    public bool TrackPointer {get;init;}=true;
    public int ControlRadius {get;init;}=8;
    public int ShadowStrength {get;init;}=100;
    public int ShadowBlur {get;init;}=24;
    public int ShadowOffset {get;init;}=10;
    public int TitleSize {get;init;}=14;
    public int TitleWeight {get;init;}=600;
    public double TitleLetterSpacing {get;init;}
    public string BorderStyle {get;init;}="solid";
    public string Font {get;init;}="default";
    public string? CustomFont {get;init;}
    public int? TextSize {get;init;}
    public double? TextLineHeight {get;init;}
    public string? BackgroundImage {get;init;}
    public string BackgroundImages {get;init;}="";
    public string[]? BackgroundMedia { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] MediaIds => (BackgroundMedia ?? ImageIds).Where(HitAppearanceService.ValidMediaId).Distinct(StringComparer.Ordinal).Take(HitAppearanceService.MaxMediaItems).ToArray();
    public string BackgroundMode {get;init;}="single";
    public int BackgroundIntervalSeconds {get;init;}=15;
    public int BackgroundTransitionMs {get;init;}=1500;
    public string BackgroundTransition {get;init;}="fade";
    public bool BackgroundWhenCollapsed {get;init;}=true;
    public double? BackgroundImageLimitMb {get;init;}=2;
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] ImageIds => new[]{BackgroundImage}.Concat((BackgroundImages??"").Split(',',StringSplitOptions.RemoveEmptyEntries)).Where(HitAppearanceService.ValidImageId).Select(id=>id!).Distinct(StringComparer.Ordinal).Take(HitAppearanceService.MaxMediaItems).ToArray();
    public int ImageOpacity {get;init;}=35;
    public string ImageFit {get;init;}="cover";
    public int BackgroundPositionX {get;init;}=50;
    public int BackgroundPositionY {get;init;}=50;
    public int BackgroundBlur {get;init;}
    public int BackgroundSaturation {get;init;}=100;
    public int BackgroundBrightness {get;init;}=100;
    public int BackgroundContrast {get;init;}=100;
    public int BackgroundGrayscale {get;init;}
    public int BackgroundZoom {get;init;}=100;
    public int BackgroundRotation {get;init;}
    public int? CollapsedBackgroundOpacity {get;init;}
    public static string QuoteFont(string family)=>"'"+string.Concat(family.Select(c=>char.IsControl(c)?$"\\{(int)c:x} ":c is '\\' or '\''?"\\"+c:c.ToString()))+"'";
    private static string? NormalizeFontName(string? family)=>family is null||family.Length>256||family.Any(char.IsControl)?null:family.Trim();
    [System.Text.Json.Serialization.JsonIgnore]
    public string? FontFamily=>Font switch
    {
        "system"=>"'Segoe UI Variable','Segoe UI',sans-serif", "arial"=>"Arial,sans-serif", "mono"=>"Consolas,monospace",
        "calibri"=>"Calibri,sans-serif", "cambria"=>"Cambria,serif", "georgia"=>"Georgia,serif", "times"=>"'Times New Roman',serif",
        "verdana"=>"Verdana,sans-serif", "tahoma"=>"Tahoma,sans-serif", "trebuchet"=>"'Trebuchet MS',sans-serif",
        "custom" when !string.IsNullOrEmpty(NormalizeFontName(CustomFont))=>QuoteFont(NormalizeFontName(CustomFont)!)+",sans-serif", _=>null
    };
    private static string Color(string? value,string fallback)=>HitColor.TryParse(value,out var color)?color.Hex:fallback;
    private static string? OptionalColor(string? value)=>value is null?null:Color(value,"#17191e");
    public HitAppearancePreferences Normalize()=>this with{Background=Color(Background,"#17191e"),Accent=Color(Accent,"#dce2ec"),Text=Color(Text,"#f1f2f4"),Border=Color(Border,"#44474f"),Radius=Math.Clamp(Radius,0,48),BorderWidth=Math.Clamp(BorderWidth,0,4),
        IdleBackground=OptionalColor(IdleBackground),Muted=OptionalColor(Muted),Eyes=OptionalColor(Eyes),EyeSize=Math.Clamp(EyeSize,3,16),EyeGap=Math.Clamp(EyeGap,0,20),ControlRadius=Math.Clamp(ControlRadius,0,24),ShadowStrength=Math.Clamp(ShadowStrength,0,150),
        BorderStyle=BorderStyle is "dashed" or "dotted"?BorderStyle:"solid",Font=Font is "system" or "mono" or "arial" or "calibri" or "cambria" or "georgia" or "times" or "verdana" or "tahoma" or "trebuchet" or "custom"?Font:"default",CustomFont=NormalizeFontName(CustomFont),BackgroundImage=HitAppearanceService.ValidImageId(BackgroundImage)?BackgroundImage:null,ImageOpacity=Math.Clamp(ImageOpacity,0,100),ImageFit=ImageFit is "contain" or "stretch"?ImageFit:"cover",
        BackgroundMedia=BackgroundMedia is null?null:MediaIds,BackgroundImages=string.Join(",",ImageIds.Where(id=>id!=BackgroundImage)),BackgroundMode=BackgroundMode is "carousel" or "smooth"?BackgroundMode:"single",BackgroundIntervalSeconds=Math.Clamp(BackgroundIntervalSeconds,5,3600),BackgroundTransitionMs=Math.Clamp(BackgroundTransitionMs,100,4000),BackgroundTransition=BackgroundTransition is "slide" or "zoom"?BackgroundTransition:"fade",
        BackgroundImageLimitMb=BackgroundImageLimitMb is null?null:double.IsFinite(BackgroundImageLimitMb.Value)?Math.Max(.25,BackgroundImageLimitMb.Value):2,
        EyeHeight=EyeHeight is null?null:Math.Clamp(EyeHeight.Value,3,24),EyeRoundness=Math.Clamp(EyeRoundness,0,50),
        ShadowBlur=Math.Clamp(ShadowBlur,0,60),ShadowOffset=Math.Clamp(ShadowOffset,0,30),TitleSize=Math.Clamp(TitleSize,10,24),TitleWeight=(int)Math.Round(Math.Clamp(TitleWeight,300,800)/100d)*100,
        TitleLetterSpacing=double.IsFinite(TitleLetterSpacing)?Math.Round(Math.Clamp(TitleLetterSpacing,0,4),1):0,
        BackgroundPositionX=Math.Clamp(BackgroundPositionX,0,100),BackgroundPositionY=Math.Clamp(BackgroundPositionY,0,100),BackgroundBlur=Math.Clamp(BackgroundBlur,0,30),BackgroundSaturation=Math.Clamp(BackgroundSaturation,0,200),
        ShadowColor=Color(ShadowColor,"#000000"),EyeOffsetX=Math.Clamp(EyeOffsetX,-16,16),EyeOffsetY=Math.Clamp(EyeOffsetY,-16,16),
        SphereSize=SphereSize is null?null:Math.Clamp(SphereSize.Value,32,128),InactiveSphereSize=InactiveSphereSize is null?null:Math.Clamp(InactiveSphereSize.Value,16,128),
        TextSize=TextSize is null?null:Math.Clamp(TextSize.Value,10,20),TextLineHeight=TextLineHeight is null||!double.IsFinite(TextLineHeight.Value)?null:Math.Round(Math.Clamp(TextLineHeight.Value,1,2.5),1),
        BackgroundBrightness=Math.Clamp(BackgroundBrightness,0,200),BackgroundContrast=Math.Clamp(BackgroundContrast,0,200),BackgroundGrayscale=Math.Clamp(BackgroundGrayscale,0,100),BackgroundZoom=Math.Clamp(BackgroundZoom,100,200),BackgroundRotation=Math.Clamp(BackgroundRotation,-180,180),CollapsedBackgroundOpacity=CollapsedBackgroundOpacity is null?null:Math.Clamp(CollapsedBackgroundOpacity.Value,0,100),EditedProperties=NormalizeEditedProperties(EditedProperties),CollapsedSphere=CollapsedSphere?.Normalize(false),InactiveSphere=InactiveSphere?.Normalize(true)};

}
public sealed record HitVisualProfile(string Id,string Name,string Theme,HitAppearancePreferences Appearance);

public sealed class HitAppearanceService
{
    public const int MaxMediaItems=256;
    public HitFontCatalog Fonts {get;}=new();
    public int MediaLimit=>_settings.Current.BackgroundMediaLimit;
    public const int MaxImageBytes=2*1024*1024;
    public string ImageDirectory {get;}
    public string VideoDirectory => Path.Combine(_directory,"Videos");
    public static bool IsVideo(string? id) => id is not null && Regex.IsMatch(id,"^[a-f0-9]{32}\\.(mp4|webm)$",RegexOptions.CultureInvariant);
    public static bool ValidMediaId(string? id) => ValidImageId(id) || IsVideo(id);
    public long VideoByteLimit => (long)Math.Ceiling(_settings.Current.BackgroundVideoLimitMb * 1_000_000);
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
        var extension=bytes.Length>=6&&(System.Text.Encoding.ASCII.GetString(bytes,0,6) is "GIF87a" or "GIF89a")?"gif":bytes.Length>=8&&bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})?"png":bytes.Length>=3&&bytes[0]==255&&bytes[1]==216&&bytes[2]==255?"jpg":bytes.Length>=12&&System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(bytes,8,4)=="WEBP"?"webp":throw new InvalidDataException(Text.Get("Choose a PNG, JPEG, WebP or GIF image."));
        if (length < 12) throw new InvalidDataException(Text.Get("Choose a valid background image."));
        if(extension=="gif"&&!GifBackgroundsAllowed)throw new InvalidDataException(Text.Get("GIF backgrounds are disabled in Settings > Background media."));
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
                    if (count > limit-total) throw new InvalidDataException(Text.Get("The image exceeds the configured size limit."));
                    await output.WriteAsync(block.AsMemory(0,count),token);total+=count;
                }
            }
            token.ThrowIfCancellationRequested();File.Move(temporary,destination);return id;
        }
        finally { if(File.Exists(temporary))File.Delete(temporary); }
    }
    public string? ImageUrl(string? id)=>ValidImageId(id)&&(GifBackgroundsAllowed||!id!.EndsWith(".gif",StringComparison.Ordinal))&&File.Exists(Path.Combine(ImageDirectory,id!))?"appearance-images/"+id:null;
    public void RemoveImportedImage(string id){if(!ValidImageId(id))throw new ArgumentException(Text.Get("Invalid image ID."));File.Delete(Path.Combine(ImageDirectory,id));}
    public string? MediaUrl(string? id) => IsVideo(id) ? File.Exists(Path.Combine(VideoDirectory,id!)) ? "appearance-videos/"+id : null : ImageUrl(id);
    public string? PosterUrl(string? id) => IsVideo(id) ? File.Exists(Path.Combine(VideoDirectory,id!+".jpg")) ? "appearance-videos/"+id+".jpg" : null : ImageUrl(id);

    public async Task<string> ImportVideoAsync(Stream source, CancellationToken token = default)
    {
        var limit=VideoByteLimit;
        var header = new byte[32]; var length = await source.ReadAtLeastAsync(header,32,false,token);
        var extension = length >= 12 && System.Text.Encoding.ASCII.GetString(header,4,4)=="ftyp" ? "mp4"
            : length >= 4 && header.AsSpan(0,4).SequenceEqual(new byte[]{0x1a,0x45,0xdf,0xa3}) ? "webm" : throw new InvalidDataException(Text.Get("Choose a valid MP4 or WebM video."));
        Directory.CreateDirectory(VideoDirectory);
        var id=Guid.NewGuid().ToString("N")+"."+extension;
        var path=Path.Combine(VideoDirectory,id); var temporary=path+".tmp";
        try
        {
            await using(var output=File.Create(temporary))
            {
                await output.WriteAsync(header.AsMemory(0,length),token);
                var buffer=new byte[81920]; long total=length; int count;
                while((count=await source.ReadAsync(buffer,token))>0)
                {
                    if(count>limit-total)throw new InvalidDataException(Text.Get("The video exceeds the configured size limit."));
                    await output.WriteAsync(buffer.AsMemory(0,count),token);total+=count;
                }
            }
            token.ThrowIfCancellationRequested();File.Move(temporary,path);return id;
        }
        finally { if(File.Exists(temporary))File.Delete(temporary); }
    }

    public async Task SaveVideoPosterAsync(string id, string dataUrl, CancellationToken token = default)
    {
        if(!IsVideo(id)||dataUrl.Length>2_000_000||!dataUrl.StartsWith("data:image/jpeg;base64,",StringComparison.Ordinal))throw new InvalidDataException(Text.Get("Invalid video preview."));
        byte[] bytes;
        try{bytes=Convert.FromBase64String(dataUrl[23..]);}
        catch(FormatException error){throw new InvalidDataException(Text.Get("Invalid video preview."),error);}
        if(bytes.Length<3||bytes[0]!=255||bytes[1]!=216||bytes[2]!=255)throw new InvalidDataException(Text.Get("Invalid video preview."));
        var path=Path.Combine(VideoDirectory,id+".jpg");
        try { await File.WriteAllBytesAsync(path+".tmp",bytes,token);token.ThrowIfCancellationRequested();File.Move(path+".tmp",path,true); }
        finally { if(File.Exists(path+".tmp"))File.Delete(path+".tmp"); }
    }

    public void RemoveImportedMedia(string id)
    {
        if(!IsVideo(id)){RemoveImportedImage(id);return;}
        File.Delete(Path.Combine(VideoDirectory,id));File.Delete(Path.Combine(VideoDirectory,id+".jpg"));
    }
    public string Css(HitAppearancePreferences appearance,bool embedImage=false)
    {
        var p=appearance.Normalize();if(!p.Enabled)return "";
        // Live video is rendered by CustomBackground. CSS exports use explicitly confirmed posters.
        var ids=p.MediaIds.Select(id=>IsVideo(id)?id+".jpg":id).ToArray();
        string? Url(string id)=>id.EndsWith(".mp4.jpg",StringComparison.Ordinal)||id.EndsWith(".webm.jpg",StringComparison.Ordinal)?PosterUrl(id[..^4]):ImageUrl(id);
        string AssetPath(string id)=>Path.Combine(IsVideo(id.EndsWith(".jpg",StringComparison.Ordinal)?id[..^4]:id)?VideoDirectory:ImageDirectory,id);
        if(embedImage)
        {
            long encodedBytes=0;
            foreach(var id in (p.BackgroundMode=="single"?ids.Take(1):ids).Where(id=>Url(id)is not null))
            {
                var size=new FileInfo(AssetPath(id)).Length;
                if(size>HitThemeService.MaxCssBytes||encodedBytes>HitThemeService.MaxCssBytes-4*((size+2)/3))throw new InvalidDataException(Text.Get("The exported theme exceeds 1 MB. Use smaller background images."));
                encodedBytes+=4*((size+2)/3);
            }
        }
        var selected=ids.FirstOrDefault(id=>Url(id)is not null);var url=selected is null?null:Url(selected);
        if(url is not null&&embedImage){var path=AssetPath(selected!);if(new FileInfo(path).Length>HitThemeService.MaxCssBytes)throw new InvalidDataException(Text.Get("The exported theme exceeds 1 MB. Use smaller background images."));var mime=Path.GetExtension(path) switch{".jpg"=>"image/jpeg",".webp"=>"image/webp",".gif"=>"image/gif",_=>"image/png"};url="data:"+mime+";base64,"+Convert.ToBase64String(File.ReadAllBytes(path));}
        if (!embedImage || p.BackgroundMode == "single" || ids.Length < 2) return p.Css(url);
        var urls = ids.Where(id=>Url(id)is not null).Select(id =>
        {
            var path = AssetPath(id);
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > HitThemeService.MaxCssBytes) throw new InvalidDataException(Text.Get("The exported theme exceeds 1 MB. Use smaller background images."));
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
        css += ".dynamic-sphere:not(.is-open) > .sphere-custom-backdrop,.dynamic-sphere:not(.is-open) > .sphere-custom-backdrop::after{animation-play-state:var(--hit-background-play-state,paused);}";
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
        if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException(Text.Get("The profile file is too large."));
        var profiles=JsonSerializer.Deserialize<HitVisualProfile[]>(File.ReadAllText(path),Json)??[];
        if(profiles.Length>50||profiles.Any(p=>p is null||!Guid.TryParse(p.Id,out _)||string.IsNullOrWhiteSpace(p.Name)||p.Name.Length>64||p.Appearance is null))throw new InvalidDataException(Text.Get("Invalid visual profiles."));
        return profiles.Select(p=>p with{Appearance=p.Appearance.Normalize()}).ToArray();
    }
    public HitVisualProfile Save(string name,HitAppearancePreferences appearance,string theme)
    {
        name=name.Trim();if(name.Length is <1 or >64||name.Any(char.IsControl))throw new ArgumentException(Text.Get("Use a profile name with 1-64 characters."));
        var profiles=Profiles();if(profiles.Length>=50)throw new InvalidOperationException(Text.Get("You can save up to 50 visual profiles."));
        var profile=new HitVisualProfile(Guid.NewGuid().ToString("N"),name,ValidTheme(theme),appearance.Normalize());
        Directory.CreateDirectory(_directory);var path=Path.Combine(_directory,"profiles.json");
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(profiles.Append(profile),Json));File.Move(path+".tmp",path,true);return profile;
    }
    public string Export(HitAppearancePreferences appearance,string theme,bool useVideoPosters=false)
    {
        if(appearance.MediaIds.Any(IsVideo)&&!useVideoPosters)throw new InvalidOperationException(Text.Get("CSS cannot play videos. Confirm export with fixed frames instead."));
        if(appearance.MediaIds.Any(id=>IsVideo(id)&&PosterUrl(id)is null))throw new InvalidOperationException(Text.Get("A video preview is missing. Import the video again before exporting."));
        var css=_themes.ReadCurrent(ValidTheme(theme)).Css+"\n"+Css(appearance,true);
        if(System.Text.Encoding.UTF8.GetByteCount(css)>HitThemeService.MaxCssBytes)throw new InvalidOperationException(Text.Get("The exported theme exceeds 1 MB."));
        var directory=Path.Combine(_directory,"Exports");Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,$"HIT-Custom-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.css");
        File.WriteAllText(path,css);return path;
    }
}
