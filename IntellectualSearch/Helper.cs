using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IntellectualSearch
{
    static class Helper
    {
        public static string SplitAndReturnLastWord(string toSplit, string regex)
        {
            Regex splitPattern = new Regex(regex);
            string[] domainSplit = splitPattern.Split(toSplit);
            return domainSplit[domainSplit.Length - 1];
        }
    }
}
