using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
using AutoYoutubePlaylist.Logic.Features.YouTube.Providers;
using AutoYoutubePlaylist.Logic.Features.YouTube.Services;

namespace AutoYoutubePlaylist.Logic.Features.Playlists.Services
{
    public class PlaylistService
        : IPlaylistService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IYouTubeService _youTubeService;
        private readonly ITodaysYouTubePlaylistNameProvider _playlistNameProvider;

        public PlaylistService(IDatabaseService databaseService, IYouTubeService youTubeService, ITodaysYouTubePlaylistNameProvider playlistNameProvider)
        {
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

        public async Task<string> TriggerPlaylistCreation()
        {
            string todaysName = _playlistNameProvider.GetName();

            YouTubePlaylist todaysPlaylist = (await _databaseService.GetAll<YouTubePlaylist>()).FirstOrDefault(x => string.Equals(x.Name, todaysName));

            if (todaysPlaylist != null)
            {
                return todaysPlaylist.FirstVideoUrl;
            }

            return await _youTubeService.CreateNewPlaylist();
        }
    }
}
