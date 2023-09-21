namespace AutoYoutubePlaylist.Logic.Features.Database.Models
{
    public interface IDatabaseEntity
    {
        Guid Id { get; set; }
        DateTime Added { get; set; }
    }
}
