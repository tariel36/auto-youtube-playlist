using System.Text.RegularExpressions;
using AutoYoutubePlaylist.Logic.Features.Configuration;
using AutoYoutubePlaylist.Logic.Features.Extensions;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
using Microsoft.Extensions.Configuration;

namespace AutoYoutubePlaylist.Logic.Features.Ordering
{
    public class VideoOrdering
    {
        public VideoOrdering(IConfiguration configuration)
        {
            string[] rules = configuration.GetSection(ConfigurationKeys.OrderingRules).Get<string[]>() ?? Array.Empty<string>();
             
            Tokens = rules.Select(OrderToken.Create).ToList();

            HasTokens = Tokens.Count > 0;
        }

        private ICollection<OrderToken> Tokens { get; }

        private bool HasTokens { get; }

        public IEnumerable<YouTubeVideo> GetOrdering(ICollection<YouTubeVideo> videos)
        {
            if (!HasTokens)
            {
                return videos;
            }

            List<YouTubeVideo> result = new List<YouTubeVideo>(videos.Count);

            HashSet<string> alreadySorted = new HashSet<string>();

            HashSet<string> specificChannels = Tokens.Select(x => x.ChannelId ?? string.Empty)
                .WhereNot(string.IsNullOrWhiteSpace)
                .ToHashSet();

            foreach (OrderToken token in Tokens)
            {
                IEnumerable<YouTubeVideo> batch = videos
                    .Where(x => !alreadySorted.Contains(x.YouTubeId))
                    .Where(x => OrderToken.IsKind(token, x));

                if (token.Kind != OrderTokenKinds.Channel)
                {
                    batch = batch.Where(x => !specificChannels.Contains(x.YouTubeId));
                }

                if (token.ExcludeShorts)
                {
                    batch = batch.Where(x => !x.IsShort);
                }

                batch = token.IsAscending
                    ? batch.OrderBy(x => x.Title)
                    : batch.OrderByDescending(x => x.Title);

                batch.ForEach(x =>
                {
                    result.Add(x);
                    alreadySorted.Add(x.YouTubeId);
                });
            }

            if (result.Count != videos.Count)
            {
                videos.Where(x => !alreadySorted.Contains(x.YouTubeId))
                    .ForEach(x => result.Add(x));
            }

            return result;
        }
        
        private class OrderToken
        {
            private const string AscendingTag = "A-Z";
            private const string DescendingTag = "Z-A";

            private static readonly Regex BaseTagRegex = new ("^(?<KIND>BASE)( (?<ORDER>(A-Z)|(Z-A)))?( (?<NO_SHORTS>-SHORTS))?$", RegexOptions.Compiled);
            private static readonly Regex ShortsTagRegex = new ("^(?<KIND>SHORTS)( (?<ORDER>(A-Z)|(Z-A)))?$", RegexOptions.Compiled);
            private static readonly Regex ChannelTagRegex = new ("^(?<KIND>CHANNEL) (?<ID>.*?)( (?<ORDER>(A-Z)|(Z-A)))?( (?<NO_SHORTS>-SHORTS))?$", RegexOptions.Compiled);

            public OrderTokenKinds Kind { get; private init; }
            
            public bool IsAscending { get; private init; }

            public bool ExcludeShorts { get; private init; }

            public string? ChannelId { get; private init; }

            public static OrderToken Create(string line)
            {
                Match match = BaseTagRegex.Match(line);

                if (!match.Success)
                {
                    match = ShortsTagRegex.Match(line);
                }

                if (!match.Success)
                {
                    match = ChannelTagRegex.Match(line);
                }

                OrderTokenKinds kind = Enum.Parse<OrderTokenKinds>(match.Groups["KIND"].Value, true);
                bool isAscending = match.Groups["ORDER"].Value == AscendingTag || string.IsNullOrEmpty(match.Groups["ORDER"].Value);
                string id = match.Groups["ID"].Value;
                bool excludeShorts = !string.IsNullOrEmpty(match.Groups["NO_SHORTS"].Value);

                return new ()
                {
                    ChannelId = id,
                    ExcludeShorts = excludeShorts,
                    IsAscending = isAscending,
                    Kind = kind
                };
            }

            public static bool IsKind(OrderToken token, YouTubeVideo video)
            {
                switch (token.Kind)
                {
                    case OrderTokenKinds.Base: return !video.IsShort;
                    case OrderTokenKinds.Shorts: return video.IsShort;
                    case OrderTokenKinds.Channel: return !string.IsNullOrWhiteSpace(token.ChannelId) && string.Equals(token.ChannelId, video.ChannelId);
                    default: return false;
                }
            }
        }

        private enum OrderTokenKinds
        {
            Unknown = 0,
            Base = 1,
            Shorts = 2,
            Channel = 3
        }
    }
}
