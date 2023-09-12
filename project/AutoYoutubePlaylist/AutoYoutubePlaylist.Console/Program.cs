using System.Diagnostics;
using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Configuration;
using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.Playlists.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
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

    ILogger logger = provider.GetRequiredService<ILogger<Program>>();

    logger.LogDebug($"Preparing services");

    IPlaylistService playlistService = provider.GetRequiredService<IPlaylistService>();
    IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
    IDatabaseService database = provider.GetRequiredService<IDatabaseService>();

    logger.LogDebug($"Starting playlist creation");

    YouTubePlaylist playlist = await playlistService.TriggerPlaylistCreation();

    logger.LogDebug($"Retrived playlist: '{playlist?.Name}' - '{playlist?.Url}' - '{playlist?.Opened}'");

    logger.LogDebug($"Should open new playlists (configuration): '{configuration[ConfigurationKeys.OpenPlaylist]}'");

    if (playlist?.Opened == false && string.Equals(configuration[ConfigurationKeys.OpenPlaylist], "1"))
    {
        playlist.Opened = true;
        
        await database.Update(playlist);

        Process.Start(new ProcessStartInfo(configuration[ConfigurationKeys.BrowserPath], playlist.FirstVideoUrl));
    }
}
