using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FateLaunchpad
{
    public static class StringExtensions
    {
        public static string[] Split(this string str, string sep)
        {
            return str.Split(new string[] { sep }, StringSplitOptions.None);
        }
    }
}
