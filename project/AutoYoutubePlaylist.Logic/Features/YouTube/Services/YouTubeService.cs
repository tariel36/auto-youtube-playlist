using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Configuration;
using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.Extensions;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
using AutoYoutubePlaylist.Logic.Features.YouTube.Providers;
using CodeHollow.FeedReader;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
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
        private readonly ITodayYouTubePlaylistNameProvider _playListNameProvider;

        public YouTubeService(ILogger<YouTubeService> logger, ITodayYouTubePlaylistNameProvider playListNameProvider, IConfiguration configuration, IDatabaseService databaseService, IDateTimeProvider dateTimeProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _playListNameProvider = playListNameProvider ?? throw new ArgumentNullException(nameof(playListNameProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<YouTubePlaylist?> CreateNewPlaylist()
        {
            _logger.LogDebug("Getting new videos...");

            ICollection<YouTubeVideo> newVideos = await GetNewVideos();

            _logger.LogDebug("New videos: '{Count}'", newVideos.Count);

            if (!(newVideos.Count > 0))
            {
                _logger.LogWarning("No videos, returning null.");

                return null;
            }

            _logger.LogDebug("Creating YT credentials");

            string? secretsFilePath = _configuration[ConfigurationKeys.ClientSecretsFilePath];

            if (string.IsNullOrWhiteSpace(secretsFilePath) || !File.Exists(secretsFilePath))
            {
                throw new InvalidOperationException("YouTube secrets file path does not exist. Set proper path to file.");
            }

            await using FileStream stream = new(secretsFilePath, FileMode.Open, FileAccess.Read);

            DateTime utcNow = _dateTimeProvider.UtcNow;
            Google.Apis.YouTube.v3.YouTubeService youtubeService;

            try
            {
                GoogleClientSecrets secrets = await GoogleClientSecrets.FromStreamAsync(stream);

                UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets.Secrets,
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
                _logger.LogError(ex, "An error occurred when trying to use authorize into Google. Try to change default web browser that has no logged in YT User and try again");
                throw new InvalidOperationException("An error occurred when trying to use authorize into Google. Try to change default web browser that has no logged in YT User and try again", ex);
            }

            await GetVideosDetails(newVideos, youtubeService);

            _logger.LogDebug("Deleting old playlists");

            await DeleteOldPlaylists(youtubeService);

            _logger.LogDebug("Creating new playlist");

            (Playlist? newPlaylist, bool youtubeErrorOccurred) = await GetNewYouTubePlaylist(youtubeService);

            if (newPlaylist == null)
            {
                _logger.LogWarning("No playlist object, returning null.");

                return null;
            }

            _logger.LogDebug("Adding videos to playlist");

            foreach (YouTubeVideo video in newVideos)
            {
                _logger.LogDebug("Processing - '{Link}'", video.Link);

                if (video.IsReleased == true)
                {
                    if (string.IsNullOrWhiteSpace(video.PlaylistId))
                    {
                        _logger.LogDebug("Video already has playlist id, skipping processing.");
                        continue;
                    }

                    _logger.LogDebug("Video is released, processing usual way.");

                    if (!youtubeErrorOccurred)
                    {
                        try
                        {
                            _logger.LogDebug("Adding video to playlist - '{Link}'", video.Link);

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

                            video.PlaylistId = newPlaylist.Id;
                        }
                        catch (Exception ex)
                        {
                            youtubeErrorOccurred = true;
                            _logger.LogError(ex,
                                "YouTube Error - New videos will be added to database, but not to playlist");
                        }
                    }

                    _logger.LogDebug("Adding video to database - '{Link}'", video.Link);
                }
                else
                {
                    _logger.LogDebug("Video has not been released yet, so just adding to DB");
                }

                await _databaseService.Insert(video);
            }

            _logger.LogDebug("Adding playlist to database...");

            string playlistUrl = $"https://www.youtube.com/playlist?list={newPlaylist.Id}";
            string firstVideoUrl = $"https://www.youtube.com/watch?v={newVideos.First().YouTubeId}&list={newPlaylist.Id}&index=1";

            YouTubePlaylist youtubePlaylist = new()
            {
                Url = playlistUrl,
                CreationDate = utcNow,
                Name = newPlaylist.Snippet.Title,
                FirstVideoUrl = firstVideoUrl
            };

            await _databaseService.Insert(youtubePlaylist);

            _logger.LogDebug("Returning...");

            return youtubePlaylist;
        }

        private static async Task<YouTubeChannel> GetRecentChannelStatus(string channelRssUrl)
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
            List<YouTubeVideo> newVideos = new();

            ICollection<YouTubeVideo> allVideos = await _databaseService.GetAll<YouTubeVideo>();

            newVideos.AddRange(await GetVideosFromRss(allVideos));

            newVideos.AddRange(allVideos.Where(x => x.Added >= _dateTimeProvider.UtcNow && (x.IsReleased == false || string.IsNullOrWhiteSpace(x.PlaylistId))));

            return newVideos;
        }

        private async Task<ICollection<YouTubeVideo>> GetVideosFromRss(ICollection<YouTubeVideo> allVideos)
        {
            List<YouTubeVideo> newVideos = new();

            ICollection<YouTubeRssUrl> urls = await _databaseService.GetAll<YouTubeRssUrl>();
            Dictionary<string, YouTubeVideo> existingVideos = allVideos.ToDictionary(k => k.YouTubeId);

            foreach (YouTubeRssUrl url in urls)
            {
                YouTubeChannel retrievedChannel = await GetRecentChannelStatus(url.Url);

                if (retrievedChannel.Videos.Count > 0)
                {
                    retrievedChannel.Videos.ForEach(x =>
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

        private async Task DeleteOldPlaylists(Google.Apis.YouTube.v3.YouTubeService youtubeService)
        {
            string? playlistOldDaysStr = _configuration[ConfigurationKeys.PlaylistOldDays];

            if (!int.TryParse(playlistOldDaysStr, out int playlistOldDays))
            {
                _logger.LogWarning("Invalid `{OldDays}` configuration. `{playlistOldDaysStr}` is not valid integer.", ConfigurationKeys.PlaylistOldDays, playlistOldDaysStr);
                return;
            }

            PlaylistListResponse? response = null;
            List<Playlist> toDelete = new();

            do
            {
                PlaylistsResource.ListRequest request = youtubeService.Playlists.List("id,snippet");
                request.Mine = true;
                request.MaxResults = 100;
                request.PageToken = response?.NextPageToken;

                response = await request.ExecuteAsync();

                DateTime oldDate = _dateTimeProvider.Now.AddDays(-playlistOldDays);

                toDelete.AddRange(response.Items.Where(x => x.Snippet.Title.StartsWith(_playListNameProvider.IdentifyingPart)).Where(x => x.Snippet.PublishedAtDateTimeOffset <= oldDate));
            }
            while (!string.IsNullOrWhiteSpace(response.NextPageToken));

            foreach (Playlist playlist in toDelete)
            {
                try
                {
                    _logger.LogDebug("Deleting `{title}` playlist", playlist.Snippet.Title);
                    await youtubeService.Playlists.Delete(playlist.Id).ExecuteAsync();
                    _logger.LogDebug("Deleted `{title}` playlist", playlist.Snippet.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "While deleting `{title}` playlist", playlist.Snippet.Title);
                }
            }
        }

        private async Task<(Playlist? playlist, bool youtubeErrorOccurred)> GetNewYouTubePlaylist(Google.Apis.YouTube.v3.YouTubeService youtubeService)
        {
            bool youtubeErrorOccurred = false;

            try
            {
                Playlist? playlist = await youtubeService.Playlists.Insert(new Playlist()
                {
                    Snippet = new PlaylistSnippet()
                    {
                        Title = _playListNameProvider.GetName(),
                    },
                    Status = new PlaylistStatus()
                    {
                        PrivacyStatus = "unlisted"
                    }
                }, "snippet,status").ExecuteAsync();

                return (playlist, youtubeErrorOccurred);
            }
            catch (Exception ex)
            {
                youtubeErrorOccurred = true;
                _logger.LogError(ex, "YouTube Error");
            }

            return (null, youtubeErrorOccurred);
        }

        private async Task GetVideosDetails(ICollection<YouTubeVideo> newVideos, Google.Apis.YouTube.v3.YouTubeService youtubeService)
        {
            HashSet<string> idsToGet = new(newVideos.Select(x => x.YouTubeId));

            VideoListResponse? response = null;

            Dictionary<string, YouTubeVideo> vidsDict = newVideos.ToDictionary(k => k.YouTubeId);

            do
            {
                VideosResource.ListRequest? request = youtubeService.Videos.List("contentDetails,liveStreamingDetails");

                request.Id = new Repeatable<string>(idsToGet);
                request.MaxResults = 100;
                request.PageToken = response?.NextPageToken;

                response = await request.ExecuteAsync();

                response.Items.ForEach(x =>
                {
                    YouTubeVideo ytVid = vidsDict[x.Id];

                    ytVid.IsReleased = x.LiveStreamingDetails == null
                                       || (x.LiveStreamingDetails.ActualEndTimeDateTimeOffset.HasValue
                                           && x.LiveStreamingDetails.ActualEndTimeDateTimeOffset.Value.ToUniversalTime()
                                               .DateTime >= _dateTimeProvider.UtcNow);

                    idsToGet.Remove(x.Id);
                });
            }
            while (!string.IsNullOrWhiteSpace(response.NextPageToken) && idsToGet.Count > 0);
        }
    }
}
