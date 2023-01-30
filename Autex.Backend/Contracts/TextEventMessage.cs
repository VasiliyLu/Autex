namespace Autex.Backend.Contracts
{
    public record TextEventMessage
    {
        public int ChannelId { get; set; }
        public TextEventType EventType { get; set; }
        public List<string> Alternatives { get; set; } = new List<string>();
    }
}
