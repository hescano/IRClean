using System;
using System.Collections.Generic;

namespace IRCleanProtocol
{
    /// <summary>
    /// Used to handle users when the client requests them
    /// </summary>
    public class UserListEventArgs:EventArgs
    {
       public List<string> Users { get; set; }
       public UserListMessageType Type { get; set; }
    }
    
    /// <summary>
    /// Enum for the different packets received
    /// </summary>
    public enum UserListMessageType
    {
        ListStart = 1,
        ListContinue = 2,
        ListEnd = 3
    }
}
