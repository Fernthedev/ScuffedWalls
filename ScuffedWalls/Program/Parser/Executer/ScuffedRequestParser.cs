using System;
using System.Collections.Generic;
using System.Linq;
using ModChart;

namespace ScuffedWalls;

/// <summary>
///     Holds objects related to the current sw file being parsed
/// </summary>
public interface IRequestParser<RequestType, ReturnType>
{
    public RequestType CurrentRequest { get; }
    public ReturnType Result { get; }
    public ReturnType GetResult();
}

public class ScuffedRequestParser : IRequestParser<ScuffedRequest, BeatMap>
{
    public static Rainbow WorkspaceRainbow = new();

    private IEnumerator<ContainerRequest> _workspaceRequestEnumerator;

    public ScuffedRequestParser(ScuffedRequest request)
    {
        Instance = this;
        CurrentRequest = request;
    }

    public static ScuffedRequestParser Instance { get; private set; }
    public List<ContainerRequest> CustomFunctions => CurrentRequest.CustomFunctionRequests;
    public List<Workspace> Workspaces { get; private set; }

    public BeatMap Result { get; private set; }

    public ScuffedRequest CurrentRequest { get; }


    /*
      
    public static T GetParam<T>(string name, T defaultval, Func<string, T> converter, Lookup<Parameter> parameters)
    {
        if (!parameters.Any(p => p.Clean.Name.Equals(name))) return defaultval;
        return converter(parameters.Where(p => p.Clean.Name.Equals(name)).First().StringData);
    }
    */
    public BeatMap GetResult()
    {
        _workspaceRequestEnumerator = CurrentRequest.WorkspaceRequests.GetEnumerator();
        Workspaces = new List<Workspace>();

        while (_workspaceRequestEnumerator.MoveNext())
        {
            var workreq = _workspaceRequestEnumerator.Current;

            if (workreq.Name != null && workreq.Name != string.Empty)
                ScuffedWalls.Print($"Workspace {Workspaces.Count()} : \"{workreq.Name}\"",
                    Color: WorkspaceRainbow.Next());
            else
                ScuffedWalls.Print($"Workspace {Workspaces.Count()}", Color: WorkspaceRainbow.Next());

            Workspaces.Add(new WorkspaceRequestParser(workreq).GetResult());
        }

        var map = Workspace.Combine(Workspaces);
        map.Prune();
        if (ScuffedWallsContainer.ScuffedConfig.IsAutoSimplifyPointDefinitionsEnabled)
            try
            {
                ScuffedWalls.Print("Simplifying Point Definitions...");
                BeatmapCompressor.SimplifyAllPointDefinitions(map);
            }
            catch (IndexOutOfRangeException e)
            {
                ScuffedWalls.Print(
                    $"Failed to simplify Point Definitions, A point definition may be missing a value? ERROR:{e.Message}",
                    ScuffedWalls.LogSeverity.Warning);
            }
            catch (Exception e)
            {
                ScuffedWalls.Print($"Failed to simplify Point Definitions ERROR:{e.Message}",
                    ScuffedWalls.LogSeverity.Warning);
            }

        Result = map;
        return map;
    }
}