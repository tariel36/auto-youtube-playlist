namespace AutoYoutubePlaylist.Logic.Features.YouTube.Providers
{
    public interface ITodaysYouTubePlaylistNameProvider
    {
        string IdentyfingPart { get; }

        string GetName();
    }
}
