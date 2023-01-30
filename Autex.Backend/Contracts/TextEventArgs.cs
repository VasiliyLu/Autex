namespace Autex.Backend.Contracts
{
    public class TextEventArgs : EventArgs
    {
        public TextEventMessage TextEventMessage { get; set; }

        public TextEventArgs(TextEventMessage textEventMessage)
        {
            TextEventMessage = textEventMessage;
        }
    }
}
