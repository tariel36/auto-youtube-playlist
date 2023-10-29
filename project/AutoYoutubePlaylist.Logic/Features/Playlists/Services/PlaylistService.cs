using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
using AutoYoutubePlaylist.Logic.Features.YouTube.Providers;
using AutoYoutubePlaylist.Logic.Features.YouTube.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Urls;
using Microsoft.Extensions.Logging;

namespace AutoYoutubePlaylist.Logic.Features.Playlists.Services
{
    public class PlaylistService
        : IPlaylistService
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IYouTubeService _youTubeService;
        private readonly ITodayYouTubePlaylistNameProvider _playListNameProvider;

        public PlaylistService(ILogger<PlaylistService> logger, IDatabaseService databaseService, IYouTubeService youTubeService, ITodayYouTubePlaylistNameProvider playListNameProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _youTubeService = youTubeService ?? throw new ArgumentNullException(nameof(youTubeService));
            _playListNameProvider = playListNameProvider ?? throw new ArgumentNullException(nameof(playListNameProvider));
        }

        public async Task AddChannel(string possibleUrl)
        {
            string actualUrl = YouTubeRssUrlFactory.GetUrl(possibleUrl);

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

        public async Task<YouTubePlaylist?> GetLatestPlaylist()
        {
            return (await _databaseService.GetAll<YouTubePlaylist>()).MaxBy(x => x.CreationDate);
        }

        public async Task<YouTubePlaylist?> TriggerPlaylistCreation()
        {
            string todayName = _playListNameProvider.GetName();

            YouTubePlaylist? todayPlaylist = (await _databaseService.GetAll<YouTubePlaylist>()).FirstOrDefault(x => string.Equals(x.Name, todayName));

            if (todayPlaylist == null)
            {
                _logger.LogDebug("Creating new playlist.");

                return await _youTubeService.CreateNewPlaylist();
            }

            _logger.LogDebug("Today's playlist: '{Name}' - '{Url}'", todayPlaylist.Name, todayPlaylist.Url);

            _logger.LogDebug("Playlist already exists, returning it.");
            
            return todayPlaylist;
        }
    }
}
