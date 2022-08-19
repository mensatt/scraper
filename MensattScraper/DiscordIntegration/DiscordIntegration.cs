using Discord;
using Discord.WebSocket;
using MensattScraper.DatabaseSupport;
using MensattScraper.Internals;

namespace MensattScraper.DiscordIntegration;

public class DiscordIntegration
{
    private readonly InternalDatabaseWrapper _internalDatabaseWrapper;
    private readonly IDatabaseWrapper _outputDatabaseWrapper;

    private readonly DiscordSocketClient _client;
    private ITextChannel? _notificationChannel;

    public DiscordIntegration(InternalDatabaseWrapper internalDatabaseWrapper,
        IDatabaseWrapper outputDatabaseWrapper)
    {
        _client = new();
        _internalDatabaseWrapper = internalDatabaseWrapper;
        _outputDatabaseWrapper = outputDatabaseWrapper;
        _internalDatabaseWrapper.ConfidenceSuggestionInsertion += OnConfidenceSuggestionInsert;
    }


    public void Init()
    {
        _outputDatabaseWrapper.ConnectAndPrepare();

        _client.Log += message =>
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        };

        _client.LoginAsync(TokenType.Bot,
            Environment.GetEnvironmentVariable("MENSATT_SCRAPER_DISCORD_TOKEN") ??
            throw new ArgumentException("Discord token not set")).Wait();
        _client.StartAsync().Wait();

        var guildId = ulong.Parse(Environment.GetEnvironmentVariable(
            "MENSATT_SCRAPER_DISCORD_GUILD") ?? throw new ArgumentException("Discord guild not set"));
        var channelId = ulong.Parse(Environment.GetEnvironmentVariable(
            "MENSATT_SCRAPER_DISCORD_CHANNEL") ?? throw new ArgumentException("Discord channel not set"));

        _client.Ready += () =>
        {
            _notificationChannel = _client.Guilds.First(x => x.Id == guildId).TextChannels
                .First(x => x.Id == channelId);
            return Task.CompletedTask;
        };

        _client.ButtonExecuted += component =>
        {
            component.UpdateAsync(props =>
            {
                var buttonRow = component.Message.Components.First();

                var disabledButtonsBuilder = new ComponentBuilder();
                foreach (var previousButton in buttonRow.Components.Cast<ButtonComponent>())
                {
                    disabledButtonsBuilder.WithButton(previousButton.Label,
                        customId: $"DISABLED: {previousButton.CustomId}", style: previousButton.Style,
                        disabled: true);
                }

                props.Components = disabledButtonsBuilder.Build();
            });

            var split = component.Data.CustomId.Split(' ');
            HandleInteraction(Guid.ParseExact(split[0], "D"), (SuggestionAction) int.Parse(split[1]));
            return Task.CompletedTask;
        };
    }

    private void HandleInteraction(Guid occurrenceId, SuggestionAction action)
    {
        switch (action)
        {
            case SuggestionAction.AcceptFirst or SuggestionAction.AcceptSecond or SuggestionAction.AcceptThird:
                var confidenceSuggestion =
                    _internalDatabaseWrapper.GetConfidenceSuggestion(occurrenceId);
                var newDish =
                    _outputDatabaseWrapper.ExecuteSelectDishNormalizedAliasByNameCommand(confidenceSuggestion
                        .Suggestions[(int) action].Item2)!.Value;
                _outputDatabaseWrapper.ExecuteUpdateOccurrenceDishByIdCommand(newDish, occurrenceId);
                _outputDatabaseWrapper.ExecuteUpdateDishAliasDishByAliasNameCommand(newDish,
                    confidenceSuggestion.CreatedDishAlias);
                _outputDatabaseWrapper.ExecuteDeleteDishByIdCommand(confidenceSuggestion.DishId);
                break;
            case SuggestionAction.Insert:
                break;
            case SuggestionAction.Discard:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        _internalDatabaseWrapper.DeleteConfidenceSuggestion(occurrenceId);
    }

    private async void OnConfidenceSuggestionInsert(object sender, ConfidenceSuggestionInsertionEventArgs args)
    {
        var confidenceSuggestion = args.ConfidenceSuggestion;

        var componentBuilder = new ComponentBuilder();
        var embedBuilder = new EmbedBuilder().WithTitle(confidenceSuggestion.CreatedDishAlias)
            .WithColor(Color.Gold).WithCurrentTimestamp();

        uint count = 0;
        foreach (var (confidence, name) in confidenceSuggestion.Suggestions)
        {
            count++;
            var fieldBuilder = new EmbedFieldBuilder().WithName($"{count}: {name}")
                .WithValue($"Confidence: {confidence}");
            embedBuilder.WithFields(fieldBuilder);

            componentBuilder.WithButton($"Accept #{count}", $"{confidenceSuggestion.OccurrenceId} {count - 1}",
                ButtonStyle.Success);
        }

        componentBuilder.WithButton("Insert as new dish",
            $"{confidenceSuggestion.OccurrenceId} {(int) SuggestionAction.Insert}");
        componentBuilder.WithButton("Discard all",
            $"{confidenceSuggestion.OccurrenceId} {(int) SuggestionAction.Discard}", ButtonStyle.Danger);
        await _notificationChannel!.SendMessageAsync(embed: embedBuilder.Build(),
            components: componentBuilder.Build());
    }
}