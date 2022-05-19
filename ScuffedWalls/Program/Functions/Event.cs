using ModChart;

namespace ScuffedWalls.Functions;

[SFunction("Event", "Light")]
public class Event : ScuffedFunction
{
    protected override void Update()
    {
        InstanceWorkspace.Lights.Add(new BeatMap.Event
        {
            _time = Time,
            _type = GetParam("type", BeatMap.Event.Type.CenterLights, p => (BeatMap.Event.Type)int.Parse(p)),
            _value = GetParam("value", BeatMap.Event.Value.OnBlue, p => (BeatMap.Event.Value)int.Parse(p)),
            _customData = UnderlyingParameters.CustomDataParse(new BeatMap.Event())._customData
        });
        RegisterChanges("_event", 1);
    }
}