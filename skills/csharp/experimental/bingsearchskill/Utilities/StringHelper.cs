using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BingSearchSkill.Utilities
{
    public class StringHelper
    {
        public static string EscapeCardString(string cardString)
        {
            return cardString.Replace("\"", "\\\"");
        }
    }
}
