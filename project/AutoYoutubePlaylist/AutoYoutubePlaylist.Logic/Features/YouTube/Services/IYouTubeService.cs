namespace AutoYoutubePlaylist.Logic.Features.YouTube.Services
{
    public interface IYouTubeService
    {
        Task<string> CreateNewPlaylist();
    }
}
