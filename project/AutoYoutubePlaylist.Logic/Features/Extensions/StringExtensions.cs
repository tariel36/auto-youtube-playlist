namespace AutoYoutubePlaylist.Logic.Features.Extensions
{
    public static class StringExtensions
    {
        private static readonly string[] LineSeparators = new[]
        {
            "\r\n",
            "\n",
        };

        public static IEnumerable<string> GetLines(this string? str, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
        {
            return (str ?? string.Empty).Split(LineSeparators, options);
        }
    }
}
