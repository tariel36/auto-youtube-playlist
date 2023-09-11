namespace AutoYoutubePlaylist.Logic.Features.Chrono.Providers
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }

        DateTime Now { get; }
    }
}
