#region Using
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

public static class AbbrevationUtility
{
    private static readonly SortedDictionary<int, string> abbrevations = new()
    {
            //{1000,"K"},
            {1000000, "M" },
            {1000000000, "B" }
        };

    private static readonly SortedDictionary<int, string> abbrevations2 = new()
    {
            {1000,"K"},
            {1000000, "M" },
            {1000000000, "B" }
        };

    public static string AbbreviateNumber(long number)
    {
        string Value = number.ToString();
        for (int i = abbrevations.Count - 1; i >= 0; i--)
        {
            KeyValuePair<int, string> pair = abbrevations.ElementAt(i);
            if (Mathf.Abs(number) >= pair.Key)
            {
                float roundedNumber = number / pair.Key;
                Value = roundedNumber.ToString("0.##") + pair.Value;
            }
        }
        return Value;
    }

    public static string AbbreviateNumberFromK(long number)
    {
        string Value = number.ToString();
        for (int i = abbrevations2.Count - 1; i >= 0; i--)
        {
            KeyValuePair<int, string> pair = abbrevations2.ElementAt(i);
            if (Mathf.Abs(number) >= pair.Key)
            {
                float roundedNumber = number / (float)pair.Key;
                Value = roundedNumber.ToString("0.##") + pair.Value;
                break;
            }
        }
        return Value;
    }
}