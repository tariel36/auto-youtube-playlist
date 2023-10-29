namespace AutoYoutubePlaylist.Logic.Features.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<TModel>(this IEnumerable<TModel> enumerable,  Action<TModel> action)
        {
            enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
            action = action ?? throw new ArgumentNullException(nameof(action));

            foreach (TModel item in enumerable)
            {
                action(item);
            }
        }
    }
}
