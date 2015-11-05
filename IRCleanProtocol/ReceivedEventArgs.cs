using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCleanProtocol
{
    public class ReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public MessageType Type { get; set; }
        public string MessageFrom { get; set; }
        public string MessageTo { get; set; }
    }

    public enum MessageType
    { 
        MESSAGE_FROM_SERVER = 1,
        MESSAGE_TO_CHANNEL = 2,
        MESSAGE_TO_ME = 3
    }
}
