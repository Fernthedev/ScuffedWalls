using System;
using System.Linq;
using System.Reflection;
using ModChart;
using ScuffedWalls.Functions;

namespace ScuffedWalls;

/// <summary>
///     Adds this FunctionRequest's results to the given workspace and returns it.
/// </summary>
public class FunctionRequestParser : IRequestParser<FunctionRequest, BeatMap>
{
    public static readonly Type[] Functions = Assembly
        .GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.Namespace == "ScuffedWalls.Functions" && t.GetCustomAttributes<SFunctionAttribute>().Any())
        .ToArray();

    private readonly Workspace _instanceWorkspace;

    public FunctionRequestParser(FunctionRequest request, Workspace instance = null, bool hideLogs = false)
    {
        HideLogs = hideLogs;
        CurrentRequest = request;
        _instanceWorkspace = instance ?? BeatMap.Empty;
    }

    public bool HideLogs { get; set; }
    public FunctionRequest CurrentRequest { get; }

    public BeatMap Result { get; private set; } = BeatMap.Empty;

    public BeatMap GetResult()
    {
        AssignableInlineVariable.Ping();
        // Parameter.UnUseAll(_request.UnderlyingParameters);

        var isCustom = ScuffedRequestParser.Instance.CurrentRequest.CustomFunctionExists(CurrentRequest.Name);

        if (!isCustom && !Functions.Any(f =>
                f.GetCustomAttributes<SFunctionAttribute>().Any(a => a.ParserName.Any(n => n == CurrentRequest.Name))))
            throw new InvalidFilterCriteriaException(
                $"Function {CurrentRequest.Name} at Beat {CurrentRequest.Time} does NOT exist, skipping");

        var func =
            isCustom
                ? typeof(CustomFunction)
                : Functions.First(f =>
                    f.BaseType == typeof(ScuffedFunction) && f.GetCustomAttributes<SFunctionAttribute>().Any(a =>
                        !a.ParserName.Contains("[NONCALLABLE]") && a.ParserName.Any(n => n == CurrentRequest.Name)));


        var repeatTime = CurrentRequest.RepeatAddTime != null
            ? float.Parse(CurrentRequest.RepeatAddTime.StringData)
            : 0.0f;
        var initialTime = CurrentRequest.Time;

        var repeatVars = new TreeList<AssignableInlineVariable>(AssignableInlineVariable.Exposer);
        var repeat = new AssignableInlineVariable("repeat", "0");
        var repeattotal = new AssignableInlineVariable("repeattotal", CurrentRequest.RepeatCount.ToString());
        var beat = new AssignableInlineVariable("time", CurrentRequest.Time.ToString());
        var originalTime = new AssignableInlineVariable("timeconst", CurrentRequest.Time.ToString());
        repeatVars.Add(repeattotal);
        repeatVars.Add(repeat);
        repeatVars.Add(beat);
        repeatVars.Add(originalTime);
        foreach (var re in CurrentRequest.Parameters) re.Variables.Register(repeatVars);
        CurrentRequest.TimeParam?.Variables.Register(repeatVars);


        Debug.TryAction(() =>
            {
                var funcInstance = (ScuffedFunction)Activator.CreateInstance(func);

                funcInstance.InstantiateSFunction(CurrentRequest, _instanceWorkspace, CurrentRequest.Time,
                    CurrentRequest.RepeatCount);


                for (var i = 0; i < CurrentRequest.RepeatCount; i++)
                {
                    repeat.StringData = i.ToString();
                    beat.StringData = CurrentRequest.Time.ToString();

                    funcInstance.SetTime(CurrentRequest.Time + i * repeatTime);
                    funcInstance.Repeat();

                    // WorkspaceRequestParser.Instance.RefreshCurrentParameters();
                }

                funcInstance.Terminate();

                if (!HideLogs)
                {
                    var stats = string.Join(", ",
                        funcInstance.Stats.Select(st => $"{st.Value} {st.Key.MakePlural(st.Value)}"));
                    ScuffedWalls.Print(
                        $"Added \"{CurrentRequest.Name}\" at beat {initialTime} {(string.IsNullOrEmpty(stats) ? "" : $"({stats})")}",
                        Color: ConsoleColor.White, OverrideStackFrame: func.Name);
                }
            },
            e =>
            {
                throw new Exception($"Error executing function {CurrentRequest.Name} at Beat {CurrentRequest.Time}",
                    e.InnerException ?? e);
            });

        Parameter.Check(CurrentRequest.UnderlyingParameters);
        Result = _instanceWorkspace;
        return _instanceWorkspace;
    }
}