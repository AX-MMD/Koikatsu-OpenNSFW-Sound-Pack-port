using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IllusionMods.Koikatsu3DSEModTools
{

public class NaturalSortComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        return StringLogicalComparer.Compare(x, y);
    }
}

public static class StringLogicalComparer
{
    public static int Compare(string str1, string str2)
    {
        if (str1 == str2)
        {
            return 0;
        }

        string[] split1 = Regex.Split(str1.Replace(" ", ""), "([0-9]+)");
        string[] split2 = Regex.Split(str2.Replace(" ", ""), "([0-9]+)");

        for (int i = 0; i < split1.Length && i < split2.Length; i++)
        {
            if (split1[i] != split2[i])
            {
                return PartCompare(split1[i], split2[i]);
            }
        }

        if (split1.Length != split2.Length)
        {
            return split1.Length < split2.Length ? -1 : 1;
        }

        return 0;
    }

    private static int PartCompare(string left, string right)
    {
        int x, y;
        if (!int.TryParse(left, out x))
        {
            return left.CompareTo(right);
        }

        if (!int.TryParse(right, out y))
        {
            return left.CompareTo(right);
        }

        return x.CompareTo(y);
    }
}

}