using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using FMBot.Bot.Builders;
using FMBot.Bot.Configurations;
using FMBot.Bot.Handlers;
using FMBot.Bot.Interfaces;
using FMBot.Bot.Models;
using FMBot.Bot.Services;
using FMBot.Bot.Services.Guild;
using FMBot.Bot.Services.ThirdParty;
using FMBot.Bot.Services.WhoKnows;
using FMBot.Domain.Models;
using FMBot.Images.Generators;
using FMBot.LastFM.Api;
using FMBot.LastFM.Repositories;
using FMBot.Persistence.EntityFrameWork;
using FMBot.Persistence.Repositories;
using FMBot.Youtube.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Discord;
using RunMode = Discord.Commands.RunMode;

namespace FMBot.Bot
{
    public class Startup
    {
        public Startup(string[] args)
        {
            var config = ConfigData.Data;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "configs"))
                .AddJsonFile("config.json", true, true)
                .AddEnvironmentVariables();

            this.Configuration = configBuilder.Build();
        }

        public IConfiguration Configuration { get; }

        public static async Task RunAsync(string[] args)
        {
            var startup = new Startup(args);

            await startup.RunAsync();
        }

        private async Task RunAsync()
        {
            var botUserId = long.Parse(this.Configuration.GetSection("Discord:BotUserId")?.Value ?? "0");

            var consoleLevel = LogEventLevel.Warning;
            var logLevel = LogEventLevel.Information;
#if DEBUG
            consoleLevel = LogEventLevel.Verbose;
            logLevel = LogEventLevel.Information;
#endif

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Environment", !string.IsNullOrEmpty(this.Configuration.GetSection("Environment")?.Value) ? this.Configuration.GetSection("Environment").Value : "unknown")
                .Enrich.WithProperty("BotUserId", botUserId)
                .WriteTo.Console(consoleLevel)
                .WriteTo.Seq(this.Configuration.GetSection("Logging:SeqServerUrl")?.Value, LogEventLevel.Information, apiKey: this.Configuration.GetSection("Logging:SeqApiKey")?.Value)
                .WriteTo.Conditional(c => ConfigData.Data.Bot.ExceptionChannelWebhookId != 0,
                    wt => wt.Discord(ConfigData.Data.Bot.ExceptionChannelWebhookId, ConfigData.Data.Bot.ExceptionChannelWebhookToken, null, LogEventLevel.Fatal))
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;
             
            Log.Information(".fmbot starting up...");

            var services = new ServiceCollection(); // Create a new instance of a service collection
            this.ConfigureServices(services);

            var provider = services.BuildServiceProvider(); // Build the service provider
            //provider.GetRequiredService<LoggingService>();      // Start the logging service
            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<InteractionHandler>();
            provider.GetRequiredService<ClientLogHandler>();
            provider.GetRequiredService<UserEventHandler>();

            await provider.GetRequiredService<StartupService>().StartAsync(); // Start the startup service

            await Task.Delay(-1); // Keep the program alive
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.Configure<BotSettings>(this.Configuration);

            var discordClient = new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 0,
                ConnectionTimeout = 240000
            });

            services
                .AddSingleton(discordClient)
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Info,
                    DefaultRunMode = RunMode.Async,
                }))
                .AddSingleton<InteractionService>()
                .AddSingleton<AlbumService>()
                .AddSingleton<AlbumBuilders>()
                .AddSingleton<ArtistBuilders>()
                .AddSingleton<AlbumRepository>()
                .AddSingleton<AdminService>()
                .AddSingleton<ArtistsService>()
                .AddSingleton<ArtistRepository>()
                .AddSingleton<CensorService>()
                .AddSingleton<CrownService>()
                .AddSingleton<CountryService>()
                .AddSingleton<CountryBuilders>()
                .AddSingleton<ClientLogHandler>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<DiscogsBuilder>()
                .AddSingleton<DiscogsService>()
                .AddSingleton<FeaturedService>()
                .AddSingleton<FriendsService>()
                .AddSingleton<FriendBuilders>()
                .AddSingleton<GeniusService>()
                .AddSingleton<GenreBuilders>()
                .AddSingleton<GenreService>()
                .AddSingleton<GuildService>()
                .AddSingleton<IGuildDisabledCommandService, GuildDisabledCommandService>()
                .AddSingleton<IChannelDisabledCommandService, ChannelDisabledCommandService>()
                .AddSingleton<IIndexService, IndexService>()
                .AddSingleton<IPrefixService, PrefixService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<IUserIndexQueue, UserIndexQueue>()
                .AddSingleton<IUserUpdateQueue, UserUpdateQueue>()
                .AddSingleton<PlayService>()
                .AddSingleton<PlayBuilder>()
                .AddSingleton<PuppeteerService>()
                .AddSingleton<Random>()
                .AddSingleton<SettingService>()
                .AddSingleton<StartupService>()
                .AddSingleton<SupporterService>()
                .AddSingleton<TimerService>()
                .AddSingleton<TimeService>()
                .AddSingleton<MusicBotService>()
                .AddSingleton<TrackBuilders>()
                .AddSingleton<TrackRepository>()
                .AddSingleton<UserEventHandler>()
                .AddSingleton<UserBuilder>()
                .AddSingleton<UserService>()
                .AddSingleton<WebhookService>()
                .AddSingleton<WhoKnowsService>()
                .AddSingleton<WhoKnowsAlbumService>()
                .AddSingleton<WhoKnowsArtistService>()
                .AddSingleton<WhoKnowsPlayService>()
                .AddSingleton<WhoKnowsTrackService>()
                .AddSingleton<YoutubeService>() // Add random to the collection
                .AddSingleton<IConfiguration>(this.Configuration);

            // These services can only be added after the config is loaded
            services
                .AddSingleton<InteractionHandler>()
                .AddSingleton<IndexRepository>()
                .AddSingleton<SmallIndexRepository>()
                .AddSingleton<UpdateRepository>()
                .AddSingleton<IUpdateService, UpdateService>();

            services.AddHttpClient<ILastfmApi, LastfmApi>();
            services.AddHttpClient<ChartService>();
            services.AddHttpClient<InvidiousApi>();
            services.AddHttpClient<LastFmRepository>();
            services.AddHttpClient<TrackService>();

            services.AddHttpClient<SpotifyService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            services.AddHttpClient<MusicBrainzService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("fmbot-discord", "1.0"));
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            services.AddHealthChecks();

            services.AddDbContextFactory<FMBotDbContext>(b =>
                b.UseNpgsql(this.Configuration["Database:ConnectionString"]));

            services.AddMemoryCache();
        }

        private static void AppUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Log.Logger != null && e.ExceptionObject is Exception exception)
            {
                UnhandledExceptions(exception);

                if (e.IsTerminating)
                {
                    Log.CloseAndFlush();
                }
            }
        }

        private static void UnhandledExceptions(Exception e)
        {
            Log.Logger?.Error(e, ".fmbot crashed");
        }
    }
}
