using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Formatting;

namespace AutoYoutubePlaylist.Logic.Features.YouTube.Providers
{
    public class TodayYouTubePlaylistNameProvider
        : ITodayYouTubePlaylistNameProvider
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public TodayYouTubePlaylistNameProvider(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public string IdentifyingPart
        {
            get
            {
                return "Subs - ";
            }
        }

        public string GetName()
        {
            return IdentifyingPart + _dateTimeProvider.Now.ToString(Formats.ShortDateFormat);
        }
    }
}
