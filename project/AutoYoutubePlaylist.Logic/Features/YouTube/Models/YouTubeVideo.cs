using AutoYoutubePlaylist.Logic.Features.Database.Models;

namespace AutoYoutubePlaylist.Logic.Features.YouTube.Models
{
    public class YouTubeVideo
        : IDatabaseEntity
    {
        public YouTubeVideo()
        {
            Id = Guid.NewGuid();
        }

        public YouTubeVideo(string youtubeId, string link, DateTime? publishDate)
        {
            YouTubeId = youtubeId ?? throw new ArgumentNullException(nameof(youtubeId));
            Link = link ?? throw new ArgumentNullException(nameof(link));
            PublishDate = publishDate ?? throw new ArgumentNullException(nameof(publishDate));
        }

        public Guid Id { get; set; }

        public string YouTubeId { get; set; } = null!;

        public string Link { get; set; } = null!;

        public DateTime? PublishDate { get; set; }

        public DateTime Added { get; set; }

        public bool IsShort { get; set; }
     
        public bool? IsReleased { get; set; }

        public string? PlaylistId { get; set; }
    }
}
