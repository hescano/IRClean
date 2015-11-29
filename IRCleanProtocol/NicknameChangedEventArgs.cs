using System;

namespace IRCleanProtocol
{
    /// <summary>
    /// Used to handle when a nickname changes in the chat.
    /// </summary>
   public class NicknameChangedEventArgs : EventArgs
    {
       public string OldNickname { get; set; }
       public string NewNickname { get; set; }
       public NicknameChangedType Type { get; set; }
    }

    /// <summary>
    /// Enum for the different nickname states
    /// </summary>
   public enum NicknameChangedType
   { 
       NickChanged = 1,
       NickAssigned = 2
   }
}
