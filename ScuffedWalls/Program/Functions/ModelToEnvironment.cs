using System.Numerics;
using ModChart;
using ModChart.Wall;
using static ModChart.BeatMap;

namespace ScuffedWalls.Functions;

[SFunction("ModelToEnvironment")]
public class ModelToEnvironment : ScuffedFunction
{
    //KDA VALUES GLOWLINE
    //float scalerX = 30;
    //float scalerY = 1.2f;
    //float scalerZ = 30;
    //float YOffset = 0f;
    //float ZOffset = 1.1f;
    //float XOffset = 0f;
    //int Index = 31;
    //REGEX @$"\[{Index}\]GlowLine \(2\)"
    protected override void Update()
    {
        var Path = GetParam("path", string.Empty,
            p => System.IO.Path.Combine(ScuffedWallsContainer.ScuffedConfig.MapFolderPath, p.RemoveWhiteSpace()));
        Path = GetParam("fullpath", Path, p => p);
        var scalerX = GetParam("scalerx", 30, CustomDataParser.FloatConverter);
        var scalerY = GetParam("scalerY", 1.2f, CustomDataParser.FloatConverter);
        var scalerZ = GetParam("scalerZ", 30, CustomDataParser.FloatConverter);
        var YOffset = GetParam("YOffset", 0, CustomDataParser.FloatConverter);
        var ZOffset = GetParam("ZOffset", 1.1f, CustomDataParser.FloatConverter);
        var XOffset = GetParam("XOffset", 0, CustomDataParser.FloatConverter);
        var Index = GetParam("Index", 31, p => int.Parse(p));
        var IndexFirstCloned = GetParam("IndexFirstCloned", 118, p => int.Parse(p));
        var RegexStatement = GetParam("Regex", @"GlowLine \(2\)", p => p);
        var OuterRegexStatement = GetParam("OuterRegex", "", p => p);

        var model = new Model(Path);

        // int Index = 31;
        string IdRegex()
        {
            return @$"{OuterRegexStatement}\[{Index}\]{RegexStatement}";
        }

        InstanceWorkspace.Environment.Add(new TreeDictionary
        {
            [_id] = IdRegex() + "$",
            [_lookupMethod] = "Regex",
            [_duplicate] = model.Objects.Length
        });
        Index = IndexFirstCloned;
        foreach (var cube in model.Objects)
        {
            var Scale = cube.Transformation.Scale;
            var preModScaleY = Scale.Y;
            Scale.X *= scalerX;
            Scale.Y *= scalerY;
            Scale.Z *= scalerZ;

            var Transform = cube.Matrix.Value.TransformLoc(new Vector3(0, -1f, 0));
            var DecomposedTransform = Transformation.fromMatrix(Transform);
            DecomposedTransform.Position *= new Vector3(-1, 1, 1);
            DecomposedTransform.Position += new Vector3(0, 0, ZOffset);
            DecomposedTransform.RotationEul *= new Vector3(1, -1, -1);


            InstanceWorkspace.Environment.Add(new TreeDictionary
            {
                [_id] = IdRegex() + @"\(Clone\)$",
                [_lookupMethod] = "Regex",
                [_localPosition] = DecomposedTransform.Position.ToFloatArray(),
                [_localRotation] = DecomposedTransform.RotationEul.ToFloatArray(),
                [_scale] = Scale.ToFloatArray()
            });

            Index++;
        }

        RegisterChanges("Duplications", model.Objects.Length);
    }
}