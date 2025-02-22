using System;
using System.Collections.Generic;
using FMBot.Domain.Models;

namespace FMBot.Bot.Models
{
    public class GuildRankingSettings
    {
        public OrderType OrderType { get; set; }

        public TimePeriod ChartTimePeriod { get; set; }

        public string TimeDescription { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public string BillboardTimeDescription { get; set; }
        public DateTime BillboardStartDateTime { get; set; }
        public DateTime BillboardEndDateTime { get; set; }

        public int AmountOfDays { get; set; }
        public int AmountOfDaysWithBillboard { get; set; }

        public string NewSearchValue { get; set; }
    }

    public enum OrderType
    {
        Playcount = 1,
        Listeners = 2
    }

    public class GuildArtist
    {
        public string ArtistName { get; set; }

        public int TotalPlaycount { get; set; }

        public int ListenerCount { get; set; }

        public List<int> ListenerUserIds { get; set; }
    }

    public class WhoKnowsArtistDto
    {
        public int UserId { get; set; }

        public string Name { get; set; }

        public int Playcount { get; set; }

        public string UserNameLastFm { get; set; }
        public DateTime? LastUsed { get; set; }

        public ulong DiscordUserId { get; set; }

        public string UserName { get; set; }

        public bool? WhoKnowsWhitelisted { get; set; }
    }

    public class WhoKnowsGlobalArtistDto
    {
        public int UserId { get; set; }

        public string Name { get; set; }

        public int Playcount { get; set; }

        public string UserNameLastFm { get; set; }

        public ulong DiscordUserId { get; set; }

        public DateTime? RegisteredLastFm { get; set; }

        public PrivacyLevel PrivacyLevel { get; set; }
    }

    public class ArtistSpotifyCoverDto
    {
        public string LastFmUrl { get; set; }

        public string SpotifyImageUrl { get; set; }
    }

    public class AffinityArtist
    {
        public int UserId { get; set; }

        public string ArtistName { get; set; }

        public long Playcount { get; set; }

        public decimal Weight { get; set; }
    }

    public class AffinityArtistResultWithUser
    {
        public string Name { get; set; }

        public decimal MatchPercentage { get; set; }

        public string LastFMUsername { get; set; }

        public ulong DiscordUserId { get; set; }

        public int UserId { get; set; }
    }

    public class ArtistSearch
    {
        public ArtistSearch(ArtistInfo artist, ResponseModel response, int? randomArtistPosition = null, long? randomArtistPlaycount = null)
        {
            this.Artist = artist;
            this.Response = response;
            this.IsRandom = randomArtistPosition.HasValue && randomArtistPlaycount.HasValue;
            this.RandomArtistPosition = randomArtistPosition + 1;
            this.RandomArtistPlaycount = randomArtistPlaycount;
        }

        public ArtistInfo Artist { get; set; }
        public ResponseModel Response { get; set; }

        public bool IsRandom { get; set; }
        public int? RandomArtistPosition { get; set; }
        public long? RandomArtistPlaycount { get; set; }
    }
}
