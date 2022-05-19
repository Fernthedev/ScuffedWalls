using System.Text.Json;
using ModChart;

namespace ScuffedWalls.Functions;

[SFunction("PointDefinition")]
internal class PointDefinition : ScuffedFunction
{
    private string name;
    private object[][] points;

    protected override void Init()
    {
        name = GetParam("name", "unimplemented_pointdefinition", p => p);
        points = GetParam("points", null, p => JsonSerializer.Deserialize<object[][]>($"[{p}]"));
    }

    protected override void Update()
    {
        InstanceWorkspace.PointDefinitions.Add(new TreeDictionary
        {
            ["_name"] = name,
            ["_points"] = points
        });
        RegisterChanges("PointDefinition", 1);
    }
}

[SFunction("AnimateTrack")]
internal class CustomEventAnimateTrack : ScuffedFunction
{
    protected override void Update()
    {
        InstanceWorkspace.CustomEvents.Add(new TreeDictionary
        {
            ["_time"] = Time,
            ["_type"] = BeatMap.AnimateTrack,
            ["_data"] = UnderlyingParameters.CustomEventsDataParse()
        });

        RegisterChanges("AnimateTrack", 1);
    }
}

[SFunction("AssignPathAnimation")]
internal class CustomEventAssignpath : ScuffedFunction
{
    protected override void Update()
    {
        InstanceWorkspace.CustomEvents.Add(new TreeDictionary
        {
            ["_time"] = Time,
            ["_type"] = BeatMap.AssignPathAnimation,
            ["_data"] = UnderlyingParameters.CustomEventsDataParse()
        });

        RegisterChanges("AssignPathAnimation", 1);
    }
}

[SFunction("AssignPlayerToTrack")]
public class CustomEventPlayerTrack : ScuffedFunction
{
    protected override void Update()
    {
        InstanceWorkspace.CustomEvents.Add(new TreeDictionary
        {
            ["_time"] = Time, ["_type"] = BeatMap.AssignPlayerToTrack,
            ["_data"] = UnderlyingParameters.CustomEventsDataParse()
        });
        RegisterChanges("AssignPlayerToTrack", 1);
    }
}

[SFunction("ParentTrack")]
public class CustomEventParent : ScuffedFunction
{
    protected override void Update()
    {
        var worldpositionstays = GetParam("worldpositionstays", true, p => bool.Parse(p));
        InstanceWorkspace.CustomEvents.Add(new TreeDictionary
        {
            ["_time"] = Time, ["_type"] = BeatMap.AssignTrackParent,
            ["_data"] = UnderlyingParameters.CustomEventsDataParse()
        });
        RegisterChanges("AssignTrackParent", 1);
    }
}

[SFunction("AssignFogTrack")]
public class CustomEventFogTrack : ScuffedFunction
{
    protected override void Update()
    {
        InstanceWorkspace.CustomEvents.Add(new TreeDictionary
        {
            ["_time"] = Time,
            ["_type"] = BeatMap.AssignFogTrack,
            ["_data"] = UnderlyingParameters.CustomEventsDataParse()
        });
        RegisterChanges("AssignFogTrack", 1);
    }
}