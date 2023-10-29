using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;

namespace AutoYoutubePlaylist.Logic.Features.YouTube.Providers
{
    public class TodaysYouTubePlaylistNameProvider
        : ITodaysYouTubePlaylistNameProvider
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public TodaysYouTubePlaylistNameProvider(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public string IdentyfingPart
        {
            get
            {
                return "Subs - ";
            }
        }

        public string GetName()
        {
            return IdentyfingPart + _dateTimeProvider.Now.ToString("yyyy-MM-dd");
        }
    }
}
