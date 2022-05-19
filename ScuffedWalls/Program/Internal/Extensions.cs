using System;
using System.Collections.Generic;
using System.Linq;
using ModChart;

namespace ScuffedWalls;

internal static class Extensions
{
    public static ICustomDataMapObject CustomDataParse(this TreeList<Parameter> parameters,
        ICustomDataMapObject instance)
    {
        return CustomDataParser.Instance.ReadToCustomData(parameters, instance);
    }

    public static TreeDictionary CustomEventsDataParse(this TreeList<Parameter> parameters)
    {
        return CustomDataParser.Instance.ReadAnimation(parameters);
    }

    public static string Remove(this string line, string pattern)
    {
        return pattern.Aggregate(line, (current, c) => current.Replace(c.ToString(), ""));
    }

    public static object? ParseDynamicStringArray(this string line)
    {
        var array = ParseSWArray(line);

        return array.Length switch
        {
            0 => null,
            1 => array[0],
            _ => array
        };
    }

    public static string[] ParseSWArray(this string array)
    {
        return array.Remove("[]\"").SplitExcludeParanthesis();
    }

    public static string[] SplitExcludeBrackets(BracketAnalyzer analyzer)
    {
        var splits = new List<int>();

        for (var i = 0; i < analyzer.FullLine.Length; i++)
            if (analyzer.IsOpeningBracket(i)) i = analyzer.GetPosOfClosingSymbol(i);
            else if (analyzer.FullLine[i] == ',') splits.Add(i);

        return analyzer.FullLine.SplitAt(splits.ToArray());
    }

    public static string[] SplitAt(this string source, int[] indexes)
    {
        indexes = indexes.OrderBy(x => x).ToArray();
        var output = new string[indexes.Length + 1];
        var lastpos = 0;

        for (var i = 0; i < indexes.Length; i++)
        {
            output[i] = source.Substring(lastpos, indexes[i] - lastpos);
            lastpos = indexes[i] + 1;
        }

        output[indexes.Length] = source[lastpos..];
        return output;
    }

    public static string[] SplitExcludeParanthesis(this string line)
    {
        return SplitExcludeBrackets(new BracketAnalyzer(line, '(', ')'));
    }

    public static TreeList<T> ToTreeList<T>(this IEnumerable<T> enumerable, Func<T, string> exposer)
    {
        return new TreeList<T>(enumerable, exposer);
    }

    /// <summary>
    ///     Attempts a deep clone of an array and all of the nested arrays, clones ICloneable
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static IEnumerable<object> CloneArray(this IEnumerable<object> array)
    {
        return array.Select(item =>
        {
            return item switch
            {
                IEnumerable<object> nestedArray => nestedArray.CloneArray(),
                ICloneable cloneable => cloneable.Clone(),
                _ => item
            };
        });
    }

    public static IEnumerable<T> CombineWith<T>(this IEnumerable<T> first, params IEnumerable<T>?[] arrays)
    {
        var list = new List<T>(first);
        foreach (var array in arrays)
            if (array != null)
                list.AddRange(array);

        return list.ToArray();
    }

    public static IEnumerable<ITimeable> GetAllBetween(this IEnumerable<ITimeable> mapObjects, float starttime,
        float endtime)
    {
        return mapObjects.Where(obj => obj._time.ToFloat() >= starttime && obj._time.ToFloat() <= endtime).ToArray();
    }

    public static string MakePlural(this string s, int amount)
    {
        return amount == 1 ? s.TrimEnd('s') : s.SetEnd('s');
    }

    public static void AddRange<K, T>(this Dictionary<K, T> dict, IEnumerable<KeyValuePair<K, T>> items)
    {
        foreach (var item in items) dict[item.Key] = item.Value;
    }

    public static List<Parameter> ToParameters(this IEnumerable<KeyValuePair<int, string>> lines)
    {
        return lines.Select(line => new Parameter(line.Value, line.Key)).ToList();
    }

    public static string SetEnd(this string s, char character)
    {
        if (s.Last() == character) return s;
        return s + character;
    }

    public static List<T> Lasts<T>(this IEnumerable<T> list)
    {
        var newlist = new List<T>(list);
        newlist.RemoveAt(0);
        return newlist;
    }

    public static string ToFileString(this DateTime time)
    {
        return $"Backup - {time.ToFileTime()}";
    }

    public static string RemoveWhiteSpace(this string whiteSpace) => whiteSpace.Trim();
}