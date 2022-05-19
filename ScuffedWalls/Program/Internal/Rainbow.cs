using System;
using System.Collections;

namespace ScuffedWalls;

public class Rainbow
{
    private readonly IEnumerator colorenum;

    private readonly ConsoleColor[] colors =
    {
        ConsoleColor.Red,
        ConsoleColor.Yellow,
        ConsoleColor.Green,
        ConsoleColor.Cyan,
        ConsoleColor.Blue,
        ConsoleColor.Magenta
    };

    public Rainbow()
    {
        colorenum = colors.GetEnumerator();
    }

    public ConsoleColor Next()
    {
        if (!colorenum.MoveNext())
        {
            colorenum.Reset();
            colorenum.MoveNext();
        }

        return (ConsoleColor)colorenum.Current;
    }

    public void PrintRainbow(string s)
    {
        foreach (var letter in s)
        {
            Next();
            Console.Write(letter);
        }

        Console.Write("\n");
        Console.ResetColor();
    }
}