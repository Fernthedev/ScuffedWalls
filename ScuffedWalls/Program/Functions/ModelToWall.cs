using System;
using System.Numerics;
using System.Text.Json;
using ModChart;
using ModChart.Wall;

namespace ScuffedWalls.Functions;

[SFunction("ModelToWall", "ModelToNote", "ModelToBomb", "Model")]
internal class ModelToWall : ScuffedFunction
{
    protected override void Update()
    {
        var parsedcustomstuff = UnderlyingParameters.CustomDataParse(new BeatMap.Obstacle());
        var isNjs = parsedcustomstuff._customData != null &&
                    parsedcustomstuff._customData["_noteJumpStartBeatOffset"] != null;
        var isNjspeed = parsedcustomstuff._customData != null &&
                        parsedcustomstuff._customData["_noteJumpMovementSpeed"] != null;


        var normal = GetParam("normal", 0, p => Convert.ToInt32(bool.Parse(p)));
        var tracks = GetParam("createtracks", true, p => bool.Parse(p));
        var defaulttrack = GetParam("defaulttrack", null, p => p);
        var preserveTime = GetParam("preservetime", false, p => bool.Parse(p));
        var hasanimation = GetParam("hasanimation", true, p => bool.Parse(p));
        var assigncamtotrack = GetParam("cameratoplayer", true, p => bool.Parse(p));
        var colormult = GetParam("colormult", 1, p => float.Parse(p));
        var Notes = GetParam("createnotes", true, p => bool.Parse(p));
        var spline = GetParam("spline", false, p => bool.Parse(p));
        var smooth = GetParam("spreadspawntime", 0, p => float.Parse(p));
        var tpye = GetParam("type", ModelSettings.TypeOverride.ModelDefined,
            p => (ModelSettings.TypeOverride)int.Parse(p));
        var alpha = GetParam("alpha", null, p => (float?)float.Parse(p));
        var thicc = GetParam("thicc", null, p => (float?)float.Parse(p));
        var setdeltapos = GetParam("setdeltaposition", false, p => bool.Parse(p));
        var setdeltascale = GetParam("setdeltascale", false, p => bool.Parse(p));
        var duration = GetParam("duration", 0, p => float.Parse(p));
        Time = GetParam("definitetime", Time, p =>
        {
            if (p.ToLower().RemoveWhiteSpace() == "beats")
            {
                if (isNjs)
                    return ScuffedWallsContainer.BPMAdjuster.GetPlaceTimeBeats(Time,
                        parsedcustomstuff._customData["_noteJumpStartBeatOffset"].ToFloat());
                return ScuffedWallsContainer.BPMAdjuster.GetPlaceTimeBeats(Time);
            }

            if (p.ToLower().RemoveWhiteSpace() == "seconds")
            {
                if (isNjs)
                    return ScuffedWallsContainer.BPMAdjuster.GetPlaceTimeBeats(
                        ScuffedWallsContainer.BPMAdjuster.ToBeat(Time),
                        parsedcustomstuff._customData["_noteJumpStartBeatOffset"].ToFloat());
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
                    parsedcustomstuff._customData["_noteJumpStartBeatOffset"].ToFloat());
            return ScuffedWallsContainer.BPMAdjuster.GetDefiniteDurationBeats(
                ScuffedWallsContainer.BPMAdjuster.ToBeat(p.ToFloat()));
        });
        duration = GetParam("definitedurationbeats", duration, p =>
        {
            if (isNjs)
                return ScuffedWallsContainer.BPMAdjuster.GetDefiniteDurationBeats(p.ToFloat(),
                    parsedcustomstuff._customData["_noteJumpStartBeatOffset"].ToFloat());
            return ScuffedWallsContainer.BPMAdjuster.GetDefiniteDurationBeats(p.ToFloat());
        });


        var MapBpm = ScuffedWallsContainer.Info["_beatsPerMinute"].ToFloat();
        var MapNjs = ScuffedWallsContainer.InfoDifficulty["_noteJumpMovementSpeed"].ToFloat();
        var MapOffset = ScuffedWallsContainer.InfoDifficulty["_noteJumpStartBeatOffset"].ToFloat();

        if (isNjs) MapOffset = parsedcustomstuff._customData["_noteJumpStartBeatOffset"].ToFloat();
        if (isNjspeed) MapNjs = parsedcustomstuff._customData["_noteJumpMovementSpeed"].ToFloat();

        var output = new BeatMap();

        var Path = GetParam("path", string.Empty,
            p => System.IO.Path.Combine(ScuffedWallsContainer.ScuffedConfig.MapFolderPath, p.RemoveWhiteSpace()));
        Path = GetParam("fullpath", Path, p => p);
        AddRefresh(Path);

        var wall = new BeatMap.Obstacle
        {
            _time = Time,
            _duration = duration,
        };
        wall._customData ??= new TreeDictionary();
        
        // by default make walls fake and uninteractable
        wall._customData["_fake"] = true;
        wall._customData["_interactable"] = false;
        
        BeatMap.Append(wall, UnderlyingParameters.CustomDataParse(new BeatMap.Obstacle()), BeatMap.AppendPriority.Low);
        
        var Delta = new Transformation
        {
            Position = GetParam("deltaposition", new Vector3(0, 0, 0),
                p => JsonSerializer.Deserialize<float[]>(p).ToVector3()),
            RotationEul = GetParam("deltarotation", new Vector3(0, 0, 0),
                p => JsonSerializer.Deserialize<float[]>(p).ToVector3()),
            Scale = GetParam("deltascale", new Vector3(1, 0, 0), p => new Vector3(float.Parse(p), 0, 0))
        };

        var settings = new ModelSettings
        {
            DefaultTrack = defaulttrack,
            PCOptimizerPro = smooth,
            Path = Path,
            Thicc = thicc,
            CreateNotes = Notes,
            DeltaTransformation = Delta,
            PreserveTime = preserveTime,
            Alpha = alpha,
            technique = (ModelSettings.Technique)normal,
            AssignCameraToPlayerTrack = assigncamtotrack,
            CreateTracks = tracks,
            Spline = spline,
            ColorMult = colormult,
            HasAnimation = hasanimation,
            ObjectOverride = tpye,
            BPM = MapBpm,
            NJS = MapNjs,
            Offset = MapOffset,
            SetDeltaScale = setdeltascale,
            SetDeltaPos = setdeltapos,
            ScaleDuration = true,
            Wall = wall
        };
        var model = new WallModel(settings);

        output.AddMap(model.Output);

        foreach (var stat in output.Stats) RegisterChanges(stat.Key, stat.Value);

        InstanceWorkspace.Add(output);
    }
}