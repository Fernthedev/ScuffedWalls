using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModChart;
using ModChart.Wall;

namespace ScuffedWalls.Functions;

[SFunction("TextToWall")]
internal class TextToWall : ScuffedFunction
{
    protected override void Update()
    {
        var parsedshit = UnderlyingParameters.CustomDataParse(new BeatMap.Obstacle());
        var isNjs = parsedshit._customData != null && parsedshit._customData["_noteJumpStartBeatOffset"] != null;
        var isNjspeed = parsedshit._customData != null && parsedshit._customData["_noteJumpMovementSpeed"] != null;
        var lines = new List<string>();

        var letting = GetParam("letting", 1, p => float.Parse(p));
        var leading = GetParam("leading", 1, p => float.Parse(p));
        var size = GetParam("size", 1, p => float.Parse(p));
        var thicc = GetParam("thicc", null, p => (float?)float.Parse(p));
        var compression = GetParam("compression", 0.1f, p => float.Parse(p));
        var shift = GetParam("shift", 1, p => float.Parse(p));
        var linelength = GetParam("maxlinelength", 1000000, p => int.Parse(p));
        var isblackempty = GetParam("isblackempty", true, p => bool.Parse(p));
        var alpha = GetParam("alpha", 1, p => float.Parse(p));
        var smooth = GetParam("spreadspawntime", 0, p => float.Parse(p));
        var Path = GetParam("path", string.Empty,
            p => System.IO.Path.Combine(ScuffedWallsContainer.ScuffedConfig.MapFolderPath, p.RemoveWhiteSpace()));
        Path = GetParam("fullpath", Path, p => p);
        AddRefresh(Path);
        var duration = GetParam("duration", 0, p => float.Parse(p));
        var tpye = GetParam("type", ModelSettings.TypeOverride.ModelDefined,
            p => (ModelSettings.TypeOverride)int.Parse(p));
        Time = GetParam("definitetime", Time, p =>
        {
            if (p.ToLower().RemoveWhiteSpace() == "beats")
            {
                if (isNjs)
                    return ScuffedWallsContainer.BPMAdjuster.GetPlaceTimeBeats(Time,
                        parsedshit._customData["_noteJumpStartBeatOffset"].ToFloat());
                return ScuffedWallsContainer.BPMAdjuster.GetPlaceTimeBeats(Time);
            }

            if (p.ToLower().RemoveWhiteSpace() == "seconds")
            {
                if (isNjs)
                    return ScuffedWallsContainer.BPMAdjuster.GetPlaceTimeBeats(
                        ScuffedWallsContainer.BPMAdjuster.ToBeat(Time),
                        parsedshit._customData["_noteJumpStartBeatOffset"].ToFloat());
                return ScuffedWallsContainer.BPMAdjuster.GetPlaceTimeBeats(
                    ScuffedWallsContainer.BPMAdjuster.ToBeat(Time));
            }

            return Time;
        });
        duration = GetParam("definitedurationseconds", duration, p =>
        {
            if (isNjs)
                return ScuffedWallsContainer.BPMAdjuster.GetDefiniteDurationBeats(
                    ScuffedWallsContainer.BPMAdjuster.ToBeat(p.ToFloat()),
                    parsedshit._customData["_noteJumpStartBeatOffset"].ToFloat());
            return ScuffedWallsContainer.BPMAdjuster.GetDefiniteDurationBeats(
                ScuffedWallsContainer.BPMAdjuster.ToBeat(p.ToFloat()));
        });
        duration = GetParam("definitedurationbeats", duration, p =>
        {
            if (isNjs)
                return ScuffedWallsContainer.BPMAdjuster.GetDefiniteDurationBeats(p.ToFloat(),
                    parsedshit._customData["_noteJumpStartBeatOffset"].ToFloat());
            return ScuffedWallsContainer.BPMAdjuster.GetDefiniteDurationBeats(p.ToFloat());
        });


        var MapBpm = ScuffedWallsContainer.Info["_beatsPerMinute"].ToFloat();
        var MapNjs = ScuffedWallsContainer.InfoDifficulty["_noteJumpMovementSpeed"].ToFloat();
        var MapOffset = ScuffedWallsContainer.InfoDifficulty["_noteJumpStartBeatOffset"].ToFloat();

        if (isNjs) MapOffset = parsedshit._customData["_noteJumpStartBeatOffset"].ToFloat();
        if (isNjspeed) MapNjs = parsedshit._customData["_noteJumpMovementSpeed"].ToFloat();

        foreach (var p in UnderlyingParameters)
            if (p.Clean.Name == "line")
            {
                lines.Add(p.StringData);
                p.WasUsed = true;
            }

        var isModel = false;
        if (new FileInfo(Path).Extension.ToLower() == ".dae") isModel = true;

        var wall = new BeatMap.Obstacle
        {
            _time = Time,
            _duration = duration,
            _customData = new TreeDictionary()
        };
        
        wall._customData["_fake"] = true;
        wall._customData["_interactable"] = false;
        
        BeatMap.Append(wall, UnderlyingParameters.CustomDataParse(new BeatMap.Obstacle()), BeatMap.AppendPriority.High);

        lines.Reverse();
        var text = new WallText(new TextSettings
        {
            ModelEnabled = isModel,
            Centered = true,
            Leading = leading,
            Letting = letting,
            Path = Path,
            Text = lines.ToArray(),
            ImageSettings = new ImageSettings
            {
                scale = size,
                shift = shift,
                PCOptimizerPro = smooth,
                alfa = alpha,
                centered = false,
                isBlackEmpty = isblackempty,
                maxPixelLength = linelength,
                thicc = thicc,
                tolerance = compression,
                Wall = wall
            },
            ModelSettings = new ModelSettings
            {
                PCOptimizerPro = smooth,
                Path = Path,
                Thicc = thicc,
                CreateNotes = false,
                DeltaTransformation = null,
                PreserveTime = false,
                Alpha = alpha,
                technique = ModelSettings.Technique.Normal,
                AssignCameraToPlayerTrack = false,
                CreateTracks = false,
                Spline = false,
                HasAnimation = false,
                ObjectOverride = tpye,
                ScaleDuration = false,
                BPM = MapBpm,

                NJS = MapNjs,
                Offset = MapOffset,
                Wall = wall
            }
        });
        InstanceWorkspace.Walls.AddRange(text.Walls);
        InstanceWorkspace.Notes.AddRange(text.Notes);
        RegisterChanges("Wall", text.Walls.Length);
        if (text.Notes.Any()) RegisterChanges("Note", text.Notes.Length);
    }
}