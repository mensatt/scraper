using Discord;
using Discord.WebSocket;

namespace MensattScraper.Discord;

public class DiscordIntegration
{
    private DiscordSocketClient _client;
    private ITextChannel _textChannel;

    private readonly Dictionary<int, MessageInteractionResponseEventArgs> _dishList = new();

    public async Task StartIntegration()
    {
        _client = new();
        _client.Log += Log;

        await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("MENSATT_SCRAPER_DISCORD_TOKEN"));
        await _client.StartAsync();

        _client.Ready += () =>
        {
            Console.WriteLine("Discord bot connected");
            _textChannel = _client.Guilds.First(x => x.Id ==
                                                     ulong.Parse(Environment.GetEnvironmentVariable(
                                                         "MENSATT_SCRAPER_DISCORD_GUILD"))).TextChannels
                .First(x => x.Id == ulong.Parse(Environment.GetEnvironmentVariable(
                    "MENSATT_SCRAPER_DISCORD_CHANNEL")));
            return Task.CompletedTask;
        };

        _client.ButtonExecuted += async component =>
        {
            await component.UpdateAsync(props =>
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

            var split = component.Data.CustomId.Split(' ').Select(int.Parse).ToArray();
            var matchingArgs = _dishList[split[0]];
            matchingArgs.Type = (MessageInteractionResponseType) (split[1] - 1);

            await component.FollowupAsync($"Input for customId={component.Data.CustomId}");

            // This is blocking, may introduce problems
            OnMessageInteractionResponse(matchingArgs);
        };
    }

    public event EventHandler<MessageInteractionResponseEventArgs> MessageInteractionResponseEvent;

    private void OnMessageInteractionResponse(MessageInteractionResponseEventArgs e)
    {
        if (MessageInteractionResponseEvent is { } handler) handler(this, e);
    }

    public async void SendNewDish(TransferData transferData)
    {
        var random = Random.Shared.Next(0, int.MaxValue);

        var componentBuilder = new ComponentBuilder();
        var embedBuilder = new EmbedBuilder().WithTitle($"Dish detected: {transferData.SanitizedDishTitle}")
            .WithColor(Color.Gold);

        uint count = 0;
        foreach (var fuzzyResult in transferData.Results)
        {
            count++;
            var fieldBuilder = new EmbedFieldBuilder().WithName($"{count}: {fuzzyResult.Matched}")
                .WithValue($"Confidence: {fuzzyResult.Score}");
            embedBuilder.WithFields(fieldBuilder);

            componentBuilder.WithButton($"Accept #{count}", $"{random} {count}", ButtonStyle.Success);
        }

        componentBuilder.WithButton("Insert as new dish", $"{random} -1");
        componentBuilder.WithButton("Discard all", $"{random} -2", ButtonStyle.Danger);

        _dishList.Add(random, new(transferData));

        await _textChannel.SendMessageAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    private static Task Log(LogMessage msg)
    {
        // TODO: Console logger
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}