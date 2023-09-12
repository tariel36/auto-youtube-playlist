using AutoYoutubePlaylist.Logic.Features.YouTube.Models;

namespace AutoYoutubePlaylist.Logic.Features.YouTube.Services
{
    public interface IYouTubeService
    {
        Task<YouTubePlaylist> CreateNewPlaylist();
    }
}
