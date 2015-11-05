using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCleanProtocol
{
    public class UserListEventArgs:EventArgs
    {
       public List<String> Users { get; set; }
       public UserListMessageType Type { get; set; }
    }

    public enum UserListMessageType
    {
        LIST_START = 1,
        LIST_CONTINUE = 2,
        LIST_END = 3
    }
}
