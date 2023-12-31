﻿using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
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

        public async Task<YouTubePlaylist> CreateNewPlaylist()
        {
            _logger.LogDebug("Getting new videos...");

            ICollection<YouTubeVideo> newVideos = await GetNewVideos();

            _logger.LogDebug($"New videos: '${newVideos?.Count}'");
            ;

            if (!(newVideos?.Count > 0))
            {
                _logger.LogWarning($"No videos, returning null.");

                return null;
            }

            _logger.LogDebug("Creating YT credentials");

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

            _logger.LogDebug("Deleting old playlists");

            await DeleteOldPlaylists(youtubeService);

            _logger.LogDebug("Creating new playlist");

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

            _logger.LogDebug("Adding videos to playlist");

            foreach (YouTubeVideo video in newVideos)
            {
                _logger.LogDebug($"Processing - '{video.Link}'");

                if (!youtubeErrorOccured)
                {
                    try
                    {
                        _logger.LogDebug($"Adding video to playlist - '{video.Link}'");

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
                        _logger.LogError(ex, "YouTube Error - New videos will be added to database, but not to playlist");
                    }
                }

                _logger.LogDebug($"Adding video to database - '{video.Link}'");

                await _databaseService.Insert(video);
            }

            if (newPlaylist == null)
            {
                _logger.LogWarning($"No playlist object, returning null.");

                return null;
            }

            _logger.LogDebug($"Adding playlist to database...");

            string playlistUrl = $"https://www.youtube.com/playlist?list={newPlaylist.Id}";
            string firstVideoUrl = $"https://www.youtube.com/watch?v={newVideos.First().YouTubeId}&list={newPlaylist.Id}&index=1";

            YouTubePlaylist youtubePlaylist = new YouTubePlaylist()
            {
                Url = playlistUrl,
                CreationDate = utcNow,
                Name = newPlaylist.Snippet.Title,
                FirstVideoUrl = firstVideoUrl
            };

            await _databaseService.Insert(youtubePlaylist);

            _logger.LogDebug($"Returning...");

            return youtubePlaylist;
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

        private async Task DeleteOldPlaylists(Google.Apis.YouTube.v3.YouTubeService youtubeService)
        {
            string playlistOldDaysStr = _configuration[ConfigurationKeys.PlaylistOldDays];

            if (!int.TryParse(playlistOldDaysStr, out int playlistOldDays))
            {
                _logger.LogWarning($"Invalid `{ConfigurationKeys.PlaylistOldDays}` configuration. `{playlistOldDaysStr}` is not valid integer.");
                return;
            }

            PlaylistListResponse response = null;
            List<Playlist> toDelete = new List<Playlist>();

            do
            {
                Google.Apis.YouTube.v3.PlaylistsResource.ListRequest request = youtubeService.Playlists.List("id,snippet");
                request.Mine = true;
                request.MaxResults = 100;
                request.PageToken = response?.NextPageToken;

                response = await request.ExecuteAsync();

                DateTime oldDate = _dateTimeProvider.Now.AddDays(-playlistOldDays);

                toDelete.AddRange(response.Items.Where(x => x.Snippet.Title.StartsWith(_playlistNameProvider.IdentyfingPart)).Where(x => x.Snippet.PublishedAtDateTimeOffset <= oldDate));
            }
            while (!string.IsNullOrWhiteSpace(response.NextPageToken));

            foreach (Playlist playlist in toDelete)
            {
                try
                {
                    _logger.LogDebug($"Deleting `{playlist.Snippet.Title}` playlist");
                    await youtubeService.Playlists.Delete(playlist.Id).ExecuteAsync();
                    _logger.LogDebug($"Deleted `{playlist.Snippet.Title}` playlist");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"While deleting `{playlist.Snippet.Title}` playlist");
                }
            }
        }
    }
}
