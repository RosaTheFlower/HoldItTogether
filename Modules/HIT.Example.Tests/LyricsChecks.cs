using System.Net;
using System.Text;
using System.Text.Json;
using HoldItTogether.Services.Audio;

internal static class LyricsChecks
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task RunAsync()
    {
        var media=new MediaSnapshot{HasMedia=true,Title="Artist - Song (Official Music Video)",Artist="ArtistVEVO",Source="Google Chrome",Duration=200};
        var handler=new ProviderHandler(request=>request.RequestUri!.AbsolutePath=="/api/get" ? new{syncedLyrics="[00:01.00]Test verse"} : null);
        using(var service=new LyricsService(new HttpClient(handler)))
        {
            var lyrics=await service.FindAsync(media,CancellationToken.None);
            Check(lyrics is {IsSynced:true,Source:"LRCLIB"},"YouTube-style titles resolve to timed lyrics");
            Check(Uri.UnescapeDataString(handler.Requests[0]).Contains("track_name=Song&artist_name=Artist"),"Queries separate the performer from the channel and remove video decorations");
        }
        handler=new ProviderHandler(request=>request.RequestUri!.AbsolutePath=="/api/search" ? new[]{new{trackName="Song",artistName="Artist",duration=160,syncedLyrics="[00:01.00]Wrong video timing",plainLyrics="First verse\nSecond verse"}} : null);
        using(var service=new LyricsService(new HttpClient(handler)))
        {
            var lyrics=await service.FindAsync(media,CancellationToken.None);
            Check(lyrics is {IsSynced:false,Source:"LRCLIB"}&&lyrics.CurrentIndex(100)==-1,"Different video duration uses readable text without false synchronization");
            var count=handler.Requests.Count;await service.FindAsync(media,CancellationToken.None);
            Check(handler.Requests.Count==count,"Plain results share the bounded positive cache");
        }
        handler=new ProviderHandler(request=>request.RequestUri!.Host=="api.lyrics.ovh" ? new{lyrics="A verse\nAnother verse"} : null);
        using(var service=new LyricsService(new HttpClient(handler)))
        {
            var lyrics=await service.FindAsync(media with{Source="Spotify",Title="Song",Artist="Artist"},CancellationToken.None);
            Check(lyrics is {IsSynced:false,Source:"Lyrics.ovh"}&&lyrics.SourceUrl!.StartsWith("https://api.lyrics.ovh/"),"Songs from Spotify can use the independent fallback with accurate attribution");
        }
        using(var service=new LyricsService(new HttpClient(new ProviderHandler(_=>null))))
        using(var cancelled=new CancellationTokenSource())
        {
            cancelled.Cancel();
            try{await service.FindAsync(media,cancelled.Token);throw new Exception("Cancellation was ignored");}catch(OperationCanceledException){}
        }
        Console.WriteLine("PASS: video metadata normalization, duration-safe plain fallback, Lyrics.ovh attribution, cache and cancellation.");
    }
    private sealed class ProviderHandler(Func<HttpRequestMessage,object?> reply):HttpMessageHandler
    {
        public List<string> Requests {get;}=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Requests.Add(request.RequestUri!.AbsoluteUri);
            var value=reply(request);
            return Task.FromResult(new HttpResponseMessage(value is null?HttpStatusCode.NotFound:HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(value),Encoding.UTF8,"application/json")});
        }
    }
}
