using System;

namespace IRCleanProtocol
{
    /// <summary>
    /// Used to handle when a message has been received
    /// </summary>
    public class ReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public MessageType Type { get; set; }
        public string MessageFrom { get; set; }
        public string MessageTo { get; set; }
    }

    /// <summary>
    /// Enum for the different message types
    /// </summary>
    public enum MessageType
    { 
        MessageFromServer = 1,
        MessageToChannel = 2,
        MessageToMe = 3
    }
}
