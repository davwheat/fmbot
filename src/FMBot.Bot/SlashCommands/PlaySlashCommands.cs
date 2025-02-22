using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using FMBot.Bot.Attributes;
using FMBot.Bot.AutoCompleteHandlers;
using FMBot.Bot.Builders;
using FMBot.Bot.Extensions;
using FMBot.Bot.Models;
using FMBot.Bot.Services;
using FMBot.Bot.Services.Guild;
using FMBot.Domain.Models;
using FMBot.LastFM.Repositories;
using SummaryAttribute = Discord.Interactions.SummaryAttribute;

namespace FMBot.Bot.SlashCommands;

public class PlaySlashCommands : InteractionModuleBase
{
    private readonly UserService _userService;
    private readonly SettingService _settingService;
    private readonly PlayBuilder _playBuilder;
    private readonly GuildService _guildService;
    private readonly LastFmRepository _lastFmRepository;

    private InteractiveService Interactivity { get; }

    private static readonly List<DateTimeOffset> StackCooldownTimer = new();
    private static readonly List<ulong> StackCooldownTarget = new();

    public PlaySlashCommands(UserService userService,
        SettingService settingService,
        PlayBuilder playBuilder,
        GuildService guildService,
        LastFmRepository lastFmRepository,
        InteractiveService interactivity)
    {
        this._userService = userService;
        this._settingService = settingService;
        this._playBuilder = playBuilder;
        this._guildService = guildService;
        this._lastFmRepository = lastFmRepository;
        this.Interactivity = interactivity;
    }

    [SlashCommand("fm", "Now Playing - Shows you or someone else their current track")]
    [UsernameSetRequired]
    public async Task NowPlayingAsync([Summary("user", "The user to show (defaults to self)")] string user = null)
    {
        var existingFmCooldown = await this._guildService.GetChannelCooldown(this.Context.Channel.Id);
        if (existingFmCooldown.HasValue)
        {
            if (StackCooldownTarget.Contains(this.Context.User.Id))
            {
                if (StackCooldownTimer[StackCooldownTarget.IndexOf(this.Context.User.Id)].AddSeconds(existingFmCooldown.Value) >= DateTimeOffset.Now)
                {
                    var secondsLeft = (int)(StackCooldownTimer[
                            StackCooldownTarget.IndexOf(this.Context.User.Id)]
                        .AddSeconds(existingFmCooldown.Value) - DateTimeOffset.Now).TotalSeconds;
                    if (secondsLeft <= existingFmCooldown.Value - 2)
                    {
                        await RespondAsync(
                            $"This channel has a `{existingFmCooldown.Value}` second cooldown on `/fm`. Please wait for this to expire before using this command again.",
                            ephemeral: true);
                    }

                    return;
                }

                StackCooldownTimer[StackCooldownTarget.IndexOf(this.Context.User.Id)] = DateTimeOffset.Now;
            }
            else
            {
                StackCooldownTarget.Add(this.Context.User.Id);
                StackCooldownTimer.Add(DateTimeOffset.Now);
            }
        }

        _ = DeferAsync();

        var contextUser = await this._userService.GetUserSettingsAsync(this.Context.User);
        var userSettings = await this._settingService.GetUser(user, contextUser, this.Context.Guild, this.Context.User, true);

        var response = await this._playBuilder.NowPlayingAsync(new ContextModel(this.Context, contextUser), userSettings);

        await this.Context.SendFollowUpResponse(this.Interactivity, response);
        this.Context.LogCommandUsed(response.CommandResponse);

        var message = await this.Context.Interaction.GetOriginalResponseAsync();

        try
        {
            if (message != null && response.CommandResponse == CommandResponse.Ok && this.Context.Guild != null)
            {
                await this._guildService.AddReactionsAsync(message, this.Context.Guild);
            }
        }
        catch (Exception e)
        {
            this.Context.LogCommandException(e, "Could not add emote reactions");
            await ReplyAsync(
                $"Couldn't add emote reactions to `/fm`. If you have recently changed changed any of the configured emotes please use `/serverreactions` to reset the automatic emote reactions.");
        }
    }

    [SlashCommand("recent", "Shows you or someone else their recent tracks")]
    [UsernameSetRequired]
    public async Task RecentAsync(
        [Summary("User", "The user to show (defaults to self)")] string user = null,
        [Summary("Amount", "Amount of recent tracks to show")] int amount = 5)
    {
        var contextUser = await this._userService.GetUserSettingsAsync(this.Context.User);
        var userSettings = await this._settingService.GetUser(user, contextUser, this.Context.Guild, this.Context.User, true);

        if (amount > 10)
        {
            amount = 10;
        }

        try
        {
            var response = await this._playBuilder.RecentAsync(new ContextModel(this.Context, contextUser),
                userSettings, amount);

            await this.Context.SendResponse(this.Interactivity, response);
            this.Context.LogCommandUsed(response.CommandResponse);
        }
        catch (Exception e)
        {
            this.Context.LogCommandException(e);
            await ReplyAsync(
                "Unable to show your recent tracks on Last.fm due to an internal error. Please try again later or contact .fmbot support.");
        }
    }

    [SlashCommand("streak", "Shows you or someone else their streak")]
    [UsernameSetRequired]
    public async Task StreakAsync(
        [Summary("User", "The user to show (defaults to self)")] string user = null)
    {
        var contextUser = await this._userService.GetUserSettingsAsync(this.Context.User);
        var userSettings = await this._settingService.GetUser(user, contextUser, this.Context.Guild, this.Context.User, true);
        var userWithStreak = await this._userService.GetUserAsync(userSettings.DiscordUserId);

        try
        {
            var response = await this._playBuilder.StreakAsync(new ContextModel(this.Context, contextUser),
                userSettings, userWithStreak);

            await this.Context.SendResponse(this.Interactivity, response);
            this.Context.LogCommandUsed(response.CommandResponse);
        }
        catch (Exception e)
        {
            this.Context.LogCommandException(e);
            await ReplyAsync(
                "Unable to show your streak on Last.fm due to an internal error. Please try again later or contact .fmbot support.");
        }
    }

    [SlashCommand("streakhistory", "Shows you or someone else their streak history")]
    [UsernameSetRequired]
    public async Task StreakHistory(
        [Summary("User", "The user to show (defaults to self)")] string user = null)
    {
        var contextUser = await this._userService.GetUserSettingsAsync(this.Context.User);
        var userSettings = await this._settingService.GetUser(user, contextUser, this.Context.Guild, this.Context.User, true);

        try
        {
            var response = await this._playBuilder.StreakHistoryAsync(new ContextModel(this.Context, contextUser), userSettings);

            await this.Context.SendResponse(this.Interactivity, response);
            this.Context.LogCommandUsed(response.CommandResponse);
        }
        catch (Exception e)
        {
            this.Context.LogCommandException(e);
            await ReplyAsync(
                "Unable to show your streak on Last.fm due to an internal error. Please try again later or contact .fmbot support.");
        }
    }

    [SlashCommand("overview", "Shows a daily overview")]
    [UsernameSetRequired]
    public async Task OverviewAsync(
        [Summary("User", "The user to show (defaults to self)")] string user = null,
        [Summary("Amount", "Amount of days to show")] int amount = 4)
    {
        _ = DeferAsync();

        var contextUser = await this._userService.GetUserSettingsAsync(this.Context.User);
        var userSettings = await this._settingService.GetUser(user, contextUser, this.Context.Guild, this.Context.User, true);

        if (amount > 8)
        {
            amount = 8;
        }

        try
        {
            var response = await this._playBuilder.OverviewAsync(new ContextModel(this.Context, contextUser),
                userSettings, amount);

            await this.Context.SendFollowUpResponse(this.Interactivity, response);
            this.Context.LogCommandUsed();
        }
        catch (Exception e)
        {
            this.Context.LogCommandException(e);
            await FollowupAsync(
                "Unable to show your overview due to an internal error. Please try again later or contact .fmbot support.",
                ephemeral: true);
        }
    }

    [SlashCommand("pace", "Shows estimated date you reach a scrobble goal based on average scrobbles per day")]
    [UsernameSetRequired]
    public async Task PaceAsync(
        [Summary("Amount", "Goal scrobble amount")] int amount = 1,
        [Summary("Time-period", "Time period to base average playcount on")][Autocomplete(typeof(DateTimeAutoComplete))] string timePeriod = null,
        [Summary("User", "The user to show (defaults to self)")] string user = null)
    {
        _ = DeferAsync();

        var contextUser = await this._userService.GetUserSettingsAsync(this.Context.User);
        var userSettings = await this._settingService.GetUser(user, contextUser, this.Context.Guild, this.Context.User, true);

        try
        {
            var userInfo = await this._lastFmRepository.GetLfmUserInfoAsync(userSettings.UserNameLastFm);
            var goalAmount = SettingService.GetGoalAmount(amount.ToString(), userInfo.Playcount);
            var timeSettings = SettingService.GetTimePeriod(timePeriod, TimePeriod.AllTime);

            long timeFrom;
            if (timeSettings.TimePeriod != TimePeriod.AllTime && timeSettings.PlayDays != null)
            {
                var dateAgo = DateTime.UtcNow.AddDays(-timeSettings.PlayDays.Value);
                timeFrom = ((DateTimeOffset)dateAgo).ToUnixTimeSeconds();
            }
            else
            {
                timeFrom = userInfo.Registered.Unixtime;
            }

            var response = await this._playBuilder.PaceAsync(new ContextModel(this.Context, contextUser),
                userSettings, timeSettings, goalAmount, userInfo.Playcount, userInfo.Registered.Unixtime);

            await this.Context.SendFollowUpResponse(this.Interactivity, response);
            this.Context.LogCommandUsed();
        }
        catch (Exception e)
        {
            this.Context.LogCommandException(e);
            await FollowupAsync(
                "Unable to show your pace due to an internal error. Please try again later or contact .fmbot support.",
                ephemeral: true);
        }
    }

    [SlashCommand("milestone", "Shows a milestone scrobble")]
    [UsernameSetRequired]
    public async Task MileStoneAsync(
        [Summary("Amount", "Milestone scrobble amount")] int amount = 99999999,
        [Summary("User", "The user to show (defaults to self)")] string user = null)
    {
        _ = DeferAsync();

        var contextUser = await this._userService.GetUserSettingsAsync(this.Context.User);
        var userSettings = await this._settingService.GetUser(user, contextUser, this.Context.Guild, this.Context.User, true);

        try
        {
            var userInfo = await this._lastFmRepository.GetLfmUserInfoAsync(userSettings.UserNameLastFm);
            var mileStoneAmount = SettingService.GetMilestoneAmount(amount.ToString(), userInfo.Playcount);

            var response = await this._playBuilder.MileStoneAsync(new ContextModel(this.Context, contextUser),
                userSettings, mileStoneAmount, userInfo.Playcount);

            await this.Context.SendFollowUpResponse(this.Interactivity, response);
            this.Context.LogCommandUsed();
        }
        catch (Exception e)
        {
            this.Context.LogCommandException(e);
            await FollowupAsync(
                "Unable to show your milestone due to an internal error. Please try again later or contact .fmbot support.",
                ephemeral: true);
        }
    }
}
