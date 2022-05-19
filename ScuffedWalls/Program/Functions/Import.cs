using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ModChart;

namespace ScuffedWalls.Functions;

[SFunction("Import")]
internal class Import : ScuffedFunction
{
    private float addtime;
    private float endbeat;
    private string Path;
    private float startbeat;
    private int[] Type;

    protected override void Init()
    {
        Path = GetParam("path", string.Empty,
            p => System.IO.Path.Combine(ScuffedWallsContainer.ScuffedConfig.MapFolderPath, p));
        Path = GetParam("fullpath", Path, p => p);
        AddRefresh(Path);
        Type = GetParam("type", new[] { 0, 1, 2, 3, 4, 5 },
            p => p.Split(",").Select(a => Convert.ToInt32(a)).ToArray());
        startbeat = Time;
        addtime = GetParam("addtime", 0, p => float.Parse(p));
        endbeat = GetParam("tobeat", float.PositiveInfinity, p => float.Parse(p));

        var beatMap = JsonSerializer.Deserialize<BeatMap>(File.ReadAllText(Path),
            ScuffedWallsContainer.DefaultJsonConverterSettings);
        var filtered = new BeatMap();

        if (beatMap._obstacles != null && beatMap._obstacles.Any() && Type.Any(t => t == 0))
            filtered._obstacles.AddRange(beatMap._obstacles.GetAllBetween(startbeat, endbeat).Select(o =>
            {
                o._time = o._time.ToFloat() + addtime;
                return o;
            }).Cast<BeatMap.Obstacle>());
        if (beatMap._notes != null && beatMap._notes.Any() && Type.Any(t => t == 1))
            filtered._notes.AddRange(beatMap._notes.GetAllBetween(startbeat, endbeat).Select(o =>
            {
                o._time = o._time.ToFloat() + addtime;
                return o;
            }).Cast<BeatMap.Note>());
        if (beatMap._events != null && beatMap._events.Any() && Type.Any(t => t == 2))
            filtered._events.AddRange(beatMap._events.GetAllBetween(startbeat, endbeat).Select(o =>
            {
                o._time = o._time.ToFloat() + addtime;
                return o;
            }).Cast<BeatMap.Event>());
        if (beatMap._customData != null && Type.Any(t => t == 3))
            TreeDictionary.Merge(InstanceWorkspace.CustomData, beatMap._customData, TreeDictionary.MergeType.Arrays);
        Stats.AddStats(filtered.Stats);
        InstanceWorkspace.Add(filtered);
    }
}