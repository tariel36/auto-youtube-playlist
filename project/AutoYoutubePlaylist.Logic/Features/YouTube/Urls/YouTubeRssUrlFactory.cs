namespace AutoYoutubePlaylist.Logic.Features.YouTube.Urls
{
    public static class YouTubeRssUrlFactory
    {
        public static string GetUrl(string possibleUrl)
        {
            return possibleUrl.StartsWith("http") ? possibleUrl : $"https://www.youtube.com/feeds/videos.xml?channel_id={possibleUrl}";
        }
    }
}
