using HIT.Sdk;
using Microsoft.Extensions.DependencyInjection;

namespace HIT.Example.Services;

public sealed class ExampleModule : IHitModule
{
    public void ConfigureServices(IServiceCollection services) => services.AddSingleton<NotesService>();
}
public sealed class NotesService
{
    public string Text { get; set; } = "";
    public bool Loaded { get; set; }
    public async Task LoadAsync(IHitApi api, CancellationToken token)
    {
        if (Loaded) return;
        Text = await api.Storage.ReadAsync<string>("note", token) ?? "";
        Loaded = true;
    }
    public Task SaveAsync(IHitApi api, CancellationToken token) => api.Storage.WriteAsync("note", Text, token);
}
