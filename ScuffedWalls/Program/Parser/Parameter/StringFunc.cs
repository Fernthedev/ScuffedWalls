using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ModChart;
using ModChart.Wall;

namespace ScuffedWalls;

public class StringFunction
{
    public static Random RandomInstance = new();
    public string Name { get; set; } //name of the func
    public Func<string, string> FunctionAction { get; set; } //convert from params to output string
    public static Func<StringFunction, string> Exposer => sfunc => sfunc.Name;

    public static StringFunction[] Functions => new[]
    {
        new()
        {
            Name = "RepeatPointDefinition",
            FunctionAction = InputArgs =>
            {
                var indexoflast = InputArgs.LastIndexOf(",");

                var pd = InputArgs.Substring(0, indexoflast);

                var repcount = int.Parse(InputArgs.Substring(indexoflast + 1, InputArgs.Length - indexoflast - 1));

                var vars = new TreeList<AssignableInlineVariable>(AssignableInlineVariable.Exposer);
                var repeat = new AssignableInlineVariable("reppd", "0");
                vars.Add(repeat);
                var computer = new StringComputationExcecuter(vars, true);

                var points = new List<string>();
                for (var i = 0; i < repcount; i++)
                {
                    repeat.StringData = i.ToString();
                    points.Add(computer.Parse(pd));
                    AssignableInlineVariable.Ping();
                }

                return string.Join(',', points);
            }
        },
        new StringFunction
        {
            Name = "MultPointDefinition",
            FunctionAction = InputArgs =>
            {
                var spli = InputArgs.Split("],", 2);
                var pd = spli[0] + "]";
                var val = spli[1].ToFloat();

                var PointDefinition = JsonSerializer.Deserialize<object[]>(pd);
                for (var i = 0; i < PointDefinition.Length; i++)
                    if (float.TryParse(PointDefinition[i].ToString(), out var result))
                        PointDefinition[i] = result * val;

                return JsonSerializer.Serialize(PointDefinition);
            }
        },
        new StringFunction
        {
            Name = "HSLtoRGB",
            FunctionAction = InputArgs =>
            {
                var parameters = InputArgs.Split(',');
                var H = parameters[0].ToFloat();
                var S = parameters.Length > 1 ? parameters[1].ToFloat() : 1f;
                var L = parameters.Length > 2 ? parameters[2].ToFloat() : 0.5f;
                var A = parameters.Length > 3 ? parameters[3].ToFloat() : 1f;
                var AdditionalValues = parameters.Length > 4
                    ? "," + string.Join(',', parameters.Slice(4, parameters.Length))
                    : string.Empty;

                var p = Color.HslToRGB(H, S, L);

                return $"[{p.R},{p.G},{p.B},{A}{AdditionalValues}]";
            }
        },
        new StringFunction
        {
            Name = "OrderPointDefinitions",
            FunctionAction = InputArgs =>
            {
                var PointDefinition = JsonSerializer.Deserialize<object[][]>($"[{InputArgs}]");
                var serial = JsonSerializer.Serialize(PointDefinition.OrderBy(p => gettimevalue(p)));
                return serial.Substring(1, serial.Length - 2);

                float gettimevalue(object[] point)
                {
                    float last = 0;
                    foreach (var val in point)
                        if (float.TryParse(val.ToString(), out var result))
                            last = result;
                    return last;
                }
            }
        },
        new StringFunction
        {
            Name = "Random",
            FunctionAction = InputArgs =>
            {
                var parameters = InputArgs.Split(',');
                var first = parameters[0].ToFloat();
                var last = parameters[1].ToFloat();
                if (parameters.Length > 2 && last < first)
                {
                    var f = first;
                    var l = last;
                    first = l;
                    last = f;
                }

                return (RandomInstance.NextDouble() * (last - first) + first).ToString();
            }
        },
        new StringFunction
        {
            Name = "RandomInt",
            FunctionAction = InputArgs =>
            {
                var parameters = InputArgs.Split(',');
                var rnd = new Random();
                var first = int.Parse(parameters[0]);
                var last = int.Parse(parameters[1]);
                if (last < first)
                {
                    var f = first;
                    var l = last;
                    first = l;
                    last = f;
                }

                return rnd.Next(first, last).ToString();
            }
        }
    };
}

public class BracketAnalyzer
{
    public char ClosingBracket;
    public string FullLine;
    public char OpeningBracket;
    public string TextAfterFocused;
    public string TextBeforeFocused;
    public string TextInsideOfBrackets;
    public string TextInsideWithBrackets;

    public BracketAnalyzer(string Line, char Opening, char Closing)
    {
        FullLine = TextInsideOfBrackets = TextInsideWithBrackets = Line;
        OpeningBracket = Opening;
        ClosingBracket = Closing;
    }

    public void Focus(int Index)
    {
        var closing = GetPosOfClosingSymbol(Index);
        var splits = SplitAt2(FullLine, Index, closing);
        TextInsideWithBrackets = splits[1];
        TextInsideOfBrackets = TextInsideWithBrackets.Substring(1, TextInsideWithBrackets.Length - 2);
        TextBeforeFocused = splits[0];
        TextAfterFocused = splits[2];
    }

    public void FocusFirst()
    {
        Focus(FullLine.IndexOf(OpeningBracket));
    }

    public void FocusFirst(string Name)
    {
        Focus(FullLine.IndexOf(Name) + (Name.Length - 1));
    }

    public bool IsOpeningBracket(int i)
    {
        return FullLine[i] == OpeningBracket;
    }

    public int GetPosOfClosingSymbol(int indexofparenthesis)
    {
        var characters = FullLine.ToCharArray();
        var depth = 0;
        for (var i = indexofparenthesis; i < characters.Length; i++)
        {
            if (characters[i] == OpeningBracket) depth++;
            else if (characters[i] == ClosingBracket) depth--;

            if (depth == 0) return i;
        }

        throw new Exception("No closing of brackets/paranthesis!");
    }

    public static string[] SplitAt2(string s, int argpos, int argpos2)
    {
        return new[] { s.Substring(0, argpos), s.Substring(argpos, argpos2 - argpos + 1), s.Substring(argpos2 + 1) };
    }
}