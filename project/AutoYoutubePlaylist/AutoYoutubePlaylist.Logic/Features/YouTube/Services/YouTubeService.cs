using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Configuration;
using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.Extensions;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
using AutoYoutubePlaylist.Logic.Features.YouTube.Providers;
using CodeHollow.FeedReader;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoYoutubePlaylist.Logic.Features.YouTube.Services
{
    public class YouTubeService
        : IYouTubeService
    {
        private readonly IConfiguration _configuration;
        private readonly IDatabaseService _databaseService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;
        private readonly ITodaysYouTubePlaylistNameProvider _playlistNameProvider;

        public YouTubeService(ILogger<YouTubeService> logger, ITodaysYouTubePlaylistNameProvider playlistNameProvider, IConfiguration configuration, IDatabaseService databaseService, IDateTimeProvider dateTimeProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _playlistNameProvider = playlistNameProvider ?? throw new ArgumentNullException(nameof(playlistNameProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<string> CreateNewPlaylist()
        {
            ICollection<YouTubeVideo> newVideos = await GetNewVideos();

            if (!(newVideos?.Count > 0))
            {
                return string.Empty;
            }

            string secretsFilePath = _configuration[ConfigurationKeys.ClientSecretsFilePath];

            if (string.IsNullOrWhiteSpace(secretsFilePath) || !File.Exists(secretsFilePath))
            {
                throw new InvalidOperationException("YouTube secrests file path does not exist. Set proper path to file.");
            }

            using FileStream stream = new FileStream(secretsFilePath, FileMode.Open, FileAccess.Read);

            DateTime utcNow = _dateTimeProvider.UtcNow;
            UserCredential credential;
            Google.Apis.YouTube.v3.YouTubeService youtubeService;

            try
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { Google.Apis.YouTube.v3.YouTubeService.Scope.Youtube },
                    _configuration[ConfigurationKeys.YouTubeUser],
                    CancellationToken.None
                );

                youtubeService = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = nameof(AutoYoutubePlaylist),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured when trying to use authorize into Google. Try to change default webbrowser that has no logged in YT User and try again");
                throw new InvalidOperationException("An error occured when trying to use authorize into Google. Try to change default webbrowser that has no logged in YT User and try again", ex);
            }

            bool youtubeErrorOccured = false;

            Playlist newPlaylist = null;

            try
            {
                newPlaylist = await youtubeService.Playlists.Insert(new Playlist()
                {
                    Snippet = new PlaylistSnippet()
                    {
                        Title = _playlistNameProvider.GetName(),
                    },
                    Status = new PlaylistStatus()
                    {
                        PrivacyStatus = "unlisted"
                    }
                }, "snippet,status").ExecuteAsync();
            }
            catch (Exception ex)
            {
                youtubeErrorOccured = true;
                _logger.LogError(ex, "YouTube Error");
            }

            foreach (YouTubeVideo video in newVideos)
            {
                if (!youtubeErrorOccured)
                {
                    try
                    {
                        await youtubeService.PlaylistItems.Insert(new PlaylistItem()
                        {
                            Snippet = new PlaylistItemSnippet()
                            {
                                PlaylistId = newPlaylist.Id,
                                ResourceId = new ResourceId()
                                {
                                    Kind = "youtube#video",
                                    VideoId = video.YouTubeId
                                }
                            }
                        }, "snippet").ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        youtubeErrorOccured = true;
                        _logger.LogError(ex, "YouTube Error");
                    }
                }

                await _databaseService.Insert(new YouTubeVideo()
                {
                    Link = video.Link,
                    YouTubeId = video.YouTubeId,
                });
            }

            if (newPlaylist == null)
            {
                return string.Empty;
            }

            string playlistUrl = $"https://www.youtube.com/playlist?list={newPlaylist.Id}";
            string firstVideoUrl = $"https://www.youtube.com/watch?v={newVideos.First().YouTubeId}&list={newPlaylist.Id}&index=1";

            await _databaseService.Insert(new YouTubePlaylist()
            {
                Url = playlistUrl,
                CreationDate = utcNow,
                Name = newPlaylist.Snippet.Title,
                FirstVideoUrl = firstVideoUrl
            });

            return firstVideoUrl;
        }

        private async Task<YouTubeChannel> GetRecentChannelStatus(string channelRssUrl)
        {
            Feed feed = await FeedReader.ReadAsync(channelRssUrl);

            string channelId = channelRssUrl.Replace("https://www.youtube.com/feeds/videos.xml?channel_id=", string.Empty);
            string channelLink = $"https://www.youtube.com/channel/{channelId}";

            return new YouTubeChannel(
                channelId,
                channelLink,
                channelRssUrl,
                feed.Items.Select(x => new YouTubeVideo(x.Id.Replace("yt:video:", string.Empty), x.Link, x.PublishingDate)).ToList()
            );
        }

        private async Task<ICollection<YouTubeVideo>> GetNewVideos()
        {
            DateTime today = _dateTimeProvider.UtcNow.Date;

            ICollection<YouTubeRssUrl> urls = await _databaseService.GetAll<YouTubeRssUrl>();
            Dictionary<string, YouTubeVideo> existingVideos = (await _databaseService.GetAll<YouTubeVideo>()).ToDictionary(k => k.YouTubeId);

            List<YouTubeVideo> newVideos = new List<YouTubeVideo>();

            foreach (YouTubeRssUrl url in urls)
            {
                YouTubeChannel retrivedChannel = await GetRecentChannelStatus(url.Url);

                if (retrivedChannel?.Videos?.Count > 0)
                {
                    retrivedChannel.Videos.ForEach(x =>
                    {
                        if (!existingVideos.ContainsKey(x.YouTubeId))
                        {
                            newVideos.Add(x);
                        }
                    });
                }
            }

            return newVideos;
        }
    }
}
