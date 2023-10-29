using System;

namespace AutoYoutubePlaylist.Gui.Features.Data.Models
{
    public class DataItem
    {
        public DataItem(string json, Guid id, DateTime added)
        {
            Json = json;
            Id = id;
            Added = added;
        }

        public string Json { get; set; }

        public string? Value { get; set; }

        public Guid Id { get; set; }

        public DateTime Added { get; set; }
    }
}
