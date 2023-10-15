using Discord;
using Discord.Webhook;

namespace waterfall.Services;

public class DiscordWebhook
{
    private static DiscordWebhookClient? WebhookClient { get; set; }

    public static void Initialize(string webhookUrl)
    {
        WebhookClient = new DiscordWebhookClient(webhookUrl);
    }

    public static async Task SendMessage(string message)
    {
        if (WebhookClient == null)
            return;

        await WebhookClient.SendMessageAsync(message);
    }

    public static async Task SendMessage(string message, Embed embed)
    {
        if (WebhookClient == null)
            return;

        await WebhookClient.SendMessageAsync(message, embeds: new[] { embed });
    }

    public static async Task SendError(string message)
    {
        if (WebhookClient == null)
            return;

        var embed = new EmbedBuilder()
            .WithTitle("Error")
            .WithDescription(message)
            .WithColor(Color.Red)
            .Build();

        await WebhookClient.SendMessageAsync("", embeds: new[] { embed });
    }

    public static async Task SendWarning(string message)
    {
        if (WebhookClient == null)
            return;

        var embed = new EmbedBuilder()
            .WithTitle("Warning")
            .WithDescription(message)
            .WithColor(Color.Orange)
            .Build();

        await WebhookClient.SendMessageAsync("", embeds: new[] { embed });
    }
}
