using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using FMBot.Bot.Configurations;
using FMBot.Bot.Extensions;
using FMBot.Bot.Models;
using FMBot.Domain.Models;
using FMBot.Persistence.Domain.Models;
using FMBot.Persistence.EntityFrameWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FMBot.Bot.Services.WhoKnows
{
    public class WhoKnowsPlayService
    {
        private readonly IDbContextFactory<FMBotDbContext> _contextFactory;
        private readonly IMemoryCache _cache;
        private readonly BotSettings _botSettings;

        public WhoKnowsPlayService(IDbContextFactory<FMBotDbContext> contextFactory, IMemoryCache cache, IOptions<BotSettings> botSettings)
        {
            this._contextFactory = contextFactory;
            this._cache = cache;
            this._botSettings = botSettings.Value;
        }

        public void AddRecentPlayToCache(int userId, RecentTrack track)
        {
            if (track.NowPlaying || track.TimePlayed != null && track.TimePlayed > DateTime.UtcNow.AddMinutes(-8))
            {
                var userPlay = new UserPlay
                {
                    ArtistName = track.ArtistName,
                    AlbumName = track.AlbumName,
                    TrackName = track.TrackName,
                    UserId = userId,
                    TimePlayed = track.TimePlayed ?? DateTime.UtcNow
                };

                this._cache.Set($"{userId}-last-play", userPlay, TimeSpan.FromMinutes(15));
            }
        }

        public string GuildAlsoPlayingTrack(int userId, Persistence.Domain.Models.Guild guild, string artistName, string trackName)
        {
            if (guild?.GuildUsers == null || !guild.GuildUsers.Any())
            {
                return null;
            }

            var foundUsers = new List<GuildUser>();
            var userPlays = new List<UserPlay>();

            foreach (var user in guild.GuildUsers.Where(w => w.UserId != userId))
            {
                var userFound = this._cache.TryGetValue($"{user.UserId}-last-play", out UserPlay userPlay);

                if (userFound && userPlay.ArtistName == artistName.ToLower() && userPlay.TrackName == trackName.ToLower())
                {
                    foundUsers.Add(user);
                    userPlays.Add(userPlay);
                }
            }

            if (!foundUsers.Any())
            {
                return null;
            }

            return foundUsers.Count switch
            {
                1 =>
                    $"{foundUsers.First().UserName} was also listening to this track {StringExtensions.GetTimeAgo(userPlays.First().TimePlayed)}!",
                2 =>
                    $"{foundUsers[0].UserName} and {foundUsers[1].UserName} were also recently listening to this track!",
                3 =>
                    $"{foundUsers[0].UserName}, {foundUsers[1].UserName} and {foundUsers[2].UserName} were also recently listening to this track!",
                > 3 =>
                    $"{foundUsers[0].UserName}, {foundUsers[1].UserName}, {foundUsers[2].UserName} and {foundUsers.Count - 3} others were also recently listening to this track!",
                _ => null
            };
        }

        public string GuildAlsoPlayingAlbum(int userId, Persistence.Domain.Models.Guild guild, string artistName, string albumName)
        {
            if (guild?.GuildUsers == null || !guild.GuildUsers.Any())
            {
                return null;
            }

            var foundUsers = new List<GuildUser>();
            var userPlays = new List<UserPlay>();

            foreach (var user in guild.GuildUsers.Where(w => w.UserId != userId))
            {
                var userFound = this._cache.TryGetValue($"{user.UserId}-last-play", out UserPlay userPlay);

                if (userFound && userPlay.ArtistName == artistName.ToLower() && userPlay.AlbumName == albumName.ToLower())
                {
                    foundUsers.Add(user);
                    userPlays.Add(userPlay);
                }
            }

            if (!foundUsers.Any())
            {
                return null;
            }

            return foundUsers.Count switch
            {
                1 =>
                    $"{foundUsers.First().UserName} was also listening to this album {StringExtensions.GetTimeAgo(userPlays.First().TimePlayed)}!",
                2 =>
                    $"{foundUsers[0].UserName} and {foundUsers[1].UserName} were also recently listening to this album!",
                3 =>
                    $"{foundUsers[0].UserName}, {foundUsers[1].UserName} and {foundUsers[2].UserName} were also recently listening to this album!",
                > 3 =>
                    $"{foundUsers[0].UserName}, {foundUsers[1].UserName}, {foundUsers[2].UserName} and {foundUsers.Count - 3} others were also recently listening to this album!",
                _ => null
            };
        }

        public string GuildAlsoPlayingArtist(int userId, Persistence.Domain.Models.Guild guild, string artistName)
        {
            if (guild?.GuildUsers == null || !guild.GuildUsers.Any())
            {
                return null;
            }

            var foundUsers = new List<GuildUser>();
            var userPlays = new List<UserPlay>();

            foreach (var user in guild.GuildUsers.Where(w => w.UserId != userId))
            {
                var userFound = this._cache.TryGetValue($"{user.UserId}-last-play", out UserPlay userPlay);

                if (userFound && userPlay.ArtistName == artistName.ToLower() && userPlay.TimePlayed > DateTime.UtcNow.AddMinutes(-10))
                {
                    foundUsers.Add(user);
                    userPlays.Add(userPlay);
                }
            }

            if (!foundUsers.Any())
            {
                return null;
            }

            return foundUsers.Count switch
            {
                1 =>
                    $"{foundUsers.First().UserName} was also listening to this artist {StringExtensions.GetTimeAgo(userPlays.First().TimePlayed)}!",
                2 =>
                    $"{foundUsers[0].UserName} and {foundUsers[1].UserName} were also recently listening to this artist!",
                3 =>
                    $"{foundUsers[0].UserName}, {foundUsers[1].UserName} and {foundUsers[2].UserName} were also recently listening to this artist!",
                > 3 =>
                    $"{foundUsers[0].UserName}, {foundUsers[1].UserName}, {foundUsers[2].UserName} and {foundUsers.Count - 3} others were also recently listening to this artist!",
                _ => null
            };
        }
    }
}
