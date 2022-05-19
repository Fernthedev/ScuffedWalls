using System;
using System.Collections.Generic;

namespace ScuffedWalls;

internal class VariableRequestParser : IRequestParser<VariableRequest, IEnumerable<AssignableInlineVariable>>
{
    public static readonly char[] VectorIndex = { 'x', 'y', 'z', 't', 'e', 's' };

    public VariableRequestParser(VariableRequest request, bool hideLogs)
    {
        HideLogs = hideLogs;
        CurrentRequest = request;
    }

    public bool HideLogs { get; set; }
    public IEnumerable<AssignableInlineVariable> Result { get; private set; }

    public VariableRequest CurrentRequest { get; }

    public IEnumerable<AssignableInlineVariable> GetResult()
    {
        var variables = new List<AssignableInlineVariable>();
        Debug.TryAction(() =>
            {
                if (CurrentRequest.ContentsType == VariableEnumType.Array ||
                    CurrentRequest.ContentsType == VariableEnumType.Vector)
                {
                    var values = CurrentRequest.Data.ParseSWArray();
                    for (var i = 0; i < values.Length; i++)
                    {
                        var indexer = CurrentRequest.ContentsType switch
                        {
                            VariableEnumType.Array => i.ToString(),
                            VariableEnumType.Vector => VectorIndex[i].ToString(),
                            _ => throw new Exception("iswimfly")
                        };
                        variables.Add(new AssignableInlineVariable(CurrentRequest.Name + $"({indexer})", values[i],
                            CurrentRequest.VariableRecomputeSettings));
                    }
                }
                else
                {
                    variables.Add(new AssignableInlineVariable(CurrentRequest.Name, CurrentRequest.Data,
                        CurrentRequest.VariableRecomputeSettings));
                    Result = variables;
                }

                if (!HideLogs)
                    foreach (var x in variables)
                        ScuffedWalls.Print($"Added Variable \"{x.Name}\" Val:{x.StringData}", ShowStackFrame: false);
            },
            e =>
            {
                ScuffedWalls.Print($"Error adding global variable {CurrentRequest.Name} ERROR:{e.Message} ",
                    ScuffedWalls.LogSeverity.Error);
            });
        Result = variables;
        return variables;
    }
}