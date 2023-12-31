﻿using AutoYoutubePlaylist.Logic.Features.YouTube.Models;

namespace AutoYoutubePlaylist.Logic.Features.Playlists.Services
{
    public interface IPlaylistService
    {
        Task AddChannel(string url);

        Task DeleteAllPlaylists();

        Task<ICollection<YouTubePlaylist>> GetAllPlaylists();

        Task<YouTubePlaylist> GetLatestPlaylist();

        Task<YouTubePlaylist> TriggerPlaylistCreation();
    }
}
