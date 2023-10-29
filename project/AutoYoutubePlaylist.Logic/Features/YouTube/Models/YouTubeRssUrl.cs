using AutoYoutubePlaylist.Logic.Features.Database.Models;

namespace AutoYoutubePlaylist.Logic.Features.YouTube.Models
{
    public class YouTubeRssUrl
        : IDatabaseEntity
    {
        public YouTubeRssUrl()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        public string Url { get; set; } = null!;

        public DateTime Added { get; set; }
    }
}
