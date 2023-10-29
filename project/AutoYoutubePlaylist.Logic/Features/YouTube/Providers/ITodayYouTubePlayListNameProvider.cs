namespace AutoYoutubePlaylist.Logic.Features.YouTube.Providers
{
    public interface ITodayYouTubePlaylistNameProvider
    {
        string IdentifyingPart { get; }

        string GetName();
    }
}
