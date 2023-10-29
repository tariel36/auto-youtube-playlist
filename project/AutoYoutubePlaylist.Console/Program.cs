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
builder.Services.AddSingleton<ITodayYouTubePlaylistNameProvider, TodayYouTubePlaylistNameProvider>();

using IHost host = builder.Build();

await CreateNewPlaylist(host.Services);

static async Task CreateNewPlaylist(IServiceProvider hostProvider)
{
    using IServiceScope serviceScope = hostProvider.CreateScope();

    IServiceProvider provider = serviceScope.ServiceProvider;

    ILogger logger = provider.GetRequiredService<ILogger<Program>>();

    logger.LogDebug("Preparing services");

    IPlaylistService playlistService = provider.GetRequiredService<IPlaylistService>();
    IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
    IDatabaseService database = provider.GetRequiredService<IDatabaseService>();

    logger.LogDebug("Starting playlist creation");

    YouTubePlaylist? playlist = await playlistService.TriggerPlaylistCreation();

    if (playlist == null)
    {
        logger.LogDebug("No playlist to process, finishing.");
        return;
    }

    logger.LogDebug("Retrieved playlist: '{PlaylistName}' - '{PlaylistNameUrl}' - '{PlaylistNameOpened}'", playlist.Name, playlist.Url, playlist.Opened);

    logger.LogDebug("Should open new Playlist (configuration): '{OpenPlaylist}'", configuration[ConfigurationKeys.OpenPlaylist]);

    if (!playlist.Opened && string.Equals(configuration[ConfigurationKeys.OpenPlaylist], "1"))
    {
        playlist.Opened = true;
        
        await database.Update(playlist);

        string? browserPath = configuration[ConfigurationKeys.BrowserPath];

        if (string.IsNullOrWhiteSpace(browserPath) || !File.Exists(browserPath))
        {
            logger.LogDebug("No browser path provided in configuration, finishing.");
            return;
        }

        Process.Start(new ProcessStartInfo(browserPath, playlist.FirstVideoUrl));
    }
}
