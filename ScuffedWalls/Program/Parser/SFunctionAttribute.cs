using System;
using System.Linq;

namespace ScuffedWalls;

[AttributeUsage(AttributeTargets.Class)]
public class SFunctionAttribute : Attribute
{
    public string Name;
    public string[] ParserName;

    public SFunctionAttribute(params string[] name)
    {
        ParserName = name.Select(n => n.ToLower().RemoveWhiteSpace()).ToArray();
        Name = name.First();
    }
}