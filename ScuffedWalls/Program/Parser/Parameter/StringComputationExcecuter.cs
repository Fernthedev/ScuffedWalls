using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;

namespace ScuffedWalls;

public class StringComputationExcecuter
{
    private static readonly TreeList<StringFunction> _stringFunctions =
        new(StringFunction.Functions, StringFunction.Exposer);

    public StringComputationExcecuter(TreeList<AssignableInlineVariable> vars = null, bool handleExceptions = false)
    {
        Variables = vars ?? new TreeList<AssignableInlineVariable>(AssignableInlineVariable.Exposer);
        HandleExceptions = handleExceptions;
    }

    public TreeList<AssignableInlineVariable> Variables { get; }
    public bool HandleExceptions { get; set; }

    public string Parse(string Line)
    {
        var LastAttempt = string.Empty;
        var ThisAttempt = Line.Clone().ToString();
        Exception MostRecentError = null;
        IEnumerable<AssignableInlineVariable> sortedVars = Variables.Values.OrderBy(v => 1f / v.Name.Length);

        while (!LastAttempt.Equals(ThisAttempt)) //if we break from this, nothing changed so there is nothing more to do
        {
            LastAttempt = ThisAttempt.Clone().ToString();

            try //Variables
            {
                var Modified = ParseVar(ThisAttempt.Clone().ToString(), sortedVars);
                if (Modified.Key) //CASE 1: string was modified with no error; last error doesnt count because it was resolved
                {
                    ThisAttempt = Modified.Value;
                    MostRecentError = null;
                }
            }
            catch (Exception e)
            {
                MostRecentError = e;
            } //CASE 3: string wasnt modified with an error; cache error, we try again later

            try //Math
            {
                var Modified = ParseMath(ThisAttempt.Clone().ToString());
                if (Modified.Key)
                {
                    ThisAttempt = Modified.Value;
                    MostRecentError = null;
                }
            }
            catch (Exception e)
            {
                MostRecentError = e;
            }

            try //Functions
            {
                var Modified = ParseFuncs(ThisAttempt.Clone().ToString());
                if (Modified.Key)
                {
                    ThisAttempt = Modified.Value;
                    MostRecentError = null;
                }
            }
            catch (Exception e)
            {
                MostRecentError = e;
            }
        }

        if (MostRecentError != null && !HandleExceptions)
            throw MostRecentError; //if there is still an error, one of the steps couldnt ever continue 

        return ThisAttempt;
    }

    public static KeyValuePair<bool, string> ParseVar(string s, IEnumerable<AssignableInlineVariable> variables)
    {
        var currentvar = "";
        var BeforeModifications = s.Clone().ToString();
        try
        {
            foreach (var v in variables)
            {
                currentvar = v.Name;
                while (s.Contains(v.Name))
                {
                    var split = s.Split(v.Name, 2);
                    s = split[0] + v.StringData + split[1];
                }
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Error implimenting variable {currentvar} ERROR:{e.Message}");
        }

        return new KeyValuePair<bool, string>(!BeforeModifications.Equals(s), s);
    }

    public static KeyValuePair<bool, string> ParseMath(string s)
    {
        var BeforeModifications = s.Clone().ToString();
        string MathStringContents = null;
        var br = new BracketAnalyzer(s, '{', '}');
        try
        {
            while (s.ToLower().Contains("{"))
            {
                br.FullLine = s;
                br.FocusFirst();
                MathStringContents = br.TextInsideOfBrackets;
                var e = new Expression(br.TextInsideOfBrackets);

                s = br.TextBeforeFocused + e.Evaluate() + br.TextAfterFocused;
            }
        }
        catch (Exception e)
        {
            //if it already made a modification, return anyways
            if (!BeforeModifications.Equals(s))
                return new KeyValuePair<bool, string>(!BeforeModifications.Equals(s), s);
            try
            {
                var Attempt = ParseMath(MathStringContents);
                if (Attempt.Key)
                    s = br.TextBeforeFocused + br.OpeningBracket + Attempt.Value + br.ClosingBracket +
                        br.TextAfterFocused;
                else throw e;
            }
            catch
            {
                throw new FormatException($"Error parsing Math {{}} Line:{s} ERROR:{e.Message}");
            }
        }

        return new KeyValuePair<bool, string>(!BeforeModifications.Equals(s), s);
    }

    public static KeyValuePair<bool, string> ParseFuncs(string s)
    {
        StringFunction currentFunc = null;
        var BeforeModifications = s.Clone().ToString();
        string FuncStringInternals = null;
        var br = new BracketAnalyzer(s, '(', ')');
        try
        {
            foreach (var func in _stringFunctions)
            {
                currentFunc = func;
                while (s.Contains(func.Name + "("))
                {
                    br.FullLine = s;
                    br.FocusFirst(func.Name + "(");
                    FuncStringInternals = br.TextInsideOfBrackets;

                    var paramss = br.TextInsideOfBrackets.Split(',');
                    s = br.TextBeforeFocused.Substring(0, br.TextBeforeFocused.Length - func.Name.Length) +
                        currentFunc.FunctionAction(br.TextInsideOfBrackets) + br.TextAfterFocused;
                }
            }
        }
        catch (Exception e)
        {
            if (!BeforeModifications.Equals(s)) return new KeyValuePair<bool, string>(true, s);
            try
            {
                var Attempt = ParseFuncs(FuncStringInternals);
                if (Attempt.Key)
                    s = br.TextBeforeFocused + br.OpeningBracket + Attempt.Value + br.ClosingBracket +
                        br.TextAfterFocused;
                else throw e;
            }
            catch
            {
                throw new Exception($"Error executing internal function {currentFunc.Name} ERROR:{e.Message}");
            }
        }

        return new KeyValuePair<bool, string>(!BeforeModifications.Equals(s), s);
    }
}