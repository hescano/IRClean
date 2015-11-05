using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public class IntegerHelper
    {
        public static Boolean IsNumeric(String value)
        {
            return value.All(Char.IsDigit);
        }
    }
