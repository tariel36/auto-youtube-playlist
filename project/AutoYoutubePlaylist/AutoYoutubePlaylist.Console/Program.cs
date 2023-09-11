using System.Diagnostics;
using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Configuration;
using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.Playlists.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Providers;
using AutoYoutubePlaylist.Logic.Features.YouTube.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(x => x.SetMinimumLevel(LogLevel.Trace).AddConsole());

builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
builder.Services.AddSingleton<IYouTubeService, YouTubeService>();
builder.Services.AddSingleton<ITodaysYouTubePlaylistNameProvider, TodaysYouTubePlaylistNameProvider>();

using IHost host = builder.Build();

await CreateNewPlaylist(host.Services);

static async Task CreateNewPlaylist(IServiceProvider hostProvider)
{
    using IServiceScope serviceScope = hostProvider.CreateScope();

    IServiceProvider provider = serviceScope.ServiceProvider;

    IPlaylistService playlistService = provider.GetRequiredService<IPlaylistService>();
    IConfiguration configuration = provider.GetRequiredService<IConfiguration>();

    string url = await playlistService.TriggerPlaylistCreation();

    if (string.Equals(configuration[ConfigurationKeys.OpenPlaylist], "1"))
    {
        Process.Start(new ProcessStartInfo(configuration[ConfigurationKeys.BrowserPath], url));
    }
}
