namespace AutoYoutubePlaylist.Logic.Features.Chrono.Providers
{
    public class DateTimeProvider
        : IDateTimeProvider
    {
        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        public DateTime Now
        {
            get { return DateTime.Now; }
        }
    }
}
