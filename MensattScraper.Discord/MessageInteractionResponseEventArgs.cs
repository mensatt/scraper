namespace MensattScraper.Discord;

public class MessageInteractionResponseEventArgs
{
    public MessageInteractionResponseEventArgs(TransferData transferData,
        MessageInteractionResponseType type = MessageInteractionResponseType.Undetermined)
    {
        Type = type;
        TransferData = transferData;
    }

    public MessageInteractionResponseType Type { set; get; }

    public TransferData TransferData { get; }
}