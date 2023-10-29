﻿using AutoYoutubePlaylist.Logic.Features.Database.Models;

namespace AutoYoutubePlaylist.Logic.Features.YouTube.Models
{
    public class YouTubePlaylist
        : IDatabaseEntity
    {
        public YouTubePlaylist()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        public string Url { get; set; } = null!;
        
        public string FirstVideoUrl { get; set; } = null!;

        public DateTime CreationDate { get; set; }

        public string Name { get; set; } = null!;

        public bool Opened { get; set; }

        public DateTime Added { get; set; }
    }
}
