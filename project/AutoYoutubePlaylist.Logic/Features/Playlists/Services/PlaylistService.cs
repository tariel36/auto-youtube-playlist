using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
using AutoYoutubePlaylist.Logic.Features.YouTube.Providers;
using AutoYoutubePlaylist.Logic.Features.YouTube.Services;
using Microsoft.Extensions.Logging;

namespace AutoYoutubePlaylist.Logic.Features.Playlists.Services
{
    public class PlaylistService
        : IPlaylistService
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IYouTubeService _youTubeService;
        private readonly ITodaysYouTubePlaylistNameProvider _playlistNameProvider;

        public PlaylistService(ILogger<PlaylistService> logger, IDatabaseService databaseService, IYouTubeService youTubeService, ITodaysYouTubePlaylistNameProvider playlistNameProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _youTubeService = youTubeService ?? throw new ArgumentNullException(nameof(youTubeService));
            _playlistNameProvider = playlistNameProvider ?? throw new ArgumentNullException(nameof(playlistNameProvider));
        }

        public async Task AddChannel(string url)
        {
            string actualUrl = url.StartsWith("http") ? url : $"https://www.youtube.com/feeds/videos.xml?channel_id={url}";

            if ((await _databaseService.GetAll<YouTubeRssUrl>()).FirstOrDefault(x => string.Equals(x.Url, actualUrl)) != null)
            {
                return;
            }

            await _databaseService.Insert(new YouTubeRssUrl()
            {
                Url = actualUrl,
            });
        }

        public async Task DeleteAllPlaylists()
        {
            await _databaseService.DeleteAll<YouTubePlaylist>();
        }

        public async Task<ICollection<YouTubePlaylist>> GetAllPlaylists()
        {
            return await _databaseService.GetAll<YouTubePlaylist>();
        }

        public async Task<YouTubePlaylist> GetLatestPlaylist()
        {
            return (await _databaseService.GetAll<YouTubePlaylist>()).OrderByDescending(x => x.CreationDate).FirstOrDefault();
        }

        public async Task<YouTubePlaylist> TriggerPlaylistCreation()
        {
            string todaysName = _playlistNameProvider.GetName();

            YouTubePlaylist todaysPlaylist = (await _databaseService.GetAll<YouTubePlaylist>()).FirstOrDefault(x => string.Equals(x.Name, todaysName));

            _logger.LogDebug($"Today's playlist: '{todaysPlaylist?.Name}' - '{todaysPlaylist?.Url}'");

            if (todaysPlaylist != null)
            {
                _logger.LogDebug("Playlist already exists, returning it.");
                return todaysPlaylist;
            }

            return await _youTubeService.CreateNewPlaylist();
        }
    }
}
