namespace AutoYoutubePlaylist.Logic.Features.YouTube.Models
{
    public class YouTubeChannel
    {
        public YouTubeChannel(string youtubeId, string url, string rssUrl, ICollection<YouTubeVideo> videos)
        {
            YouTubeId = youtubeId ?? throw new ArgumentNullException(nameof(youtubeId));
            Url = url ?? throw new ArgumentNullException(nameof(url));
            RssUrl = rssUrl ?? throw new ArgumentNullException(nameof(rssUrl));
            Videos = videos ?? throw new ArgumentNullException(nameof(videos));
        }
        
        public string YouTubeId { get; set; }

        public string Url { get; set; }

        public string RssUrl { get; set; }

        public ICollection<YouTubeVideo> Videos { get; set; }
    }
}
