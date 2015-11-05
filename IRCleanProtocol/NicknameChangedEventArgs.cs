using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCleanProtocol
{
   public class NicknameChangedEventArgs : EventArgs
    {
       public string OldNickname { get; set; }
       public string NewNickname { get; set; }
       public NicknameChangedType Type { get; set; }
    }

   public enum NicknameChangedType
   { 
       NICK_CHANGED = 1,
       NICK_ASSIGNED = 2
   }
}
