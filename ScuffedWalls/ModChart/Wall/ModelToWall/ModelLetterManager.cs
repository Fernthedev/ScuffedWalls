using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ScuffedWalls;

namespace ModChart.Wall;

internal class ModelLetterManager : IPlaceableLetterWallCollection
{
    private static readonly float scalefactor = 4;
    public Cube[]? Cubes;
    public TextSettings? Settings { get; set; }
    public Alphabet Character { get; set; }
    public Vector2 Dimensions { get; set; }

    /// <summary>
    ///     Extracts the walls and transforms them to this position
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public IEnumerable<ICustomDataMapObject> PlaceAt(Vector2 pos)
    {
        var transCubes = Cube.TransformCollection(new DeltaTransformOptions
        {
            cubes = Cubes,
            Position = new Vector3(-pos.X, pos.Y, 0)
        });
        var extracted = new WallModel(transCubes.ToArray(), Settings.ModelSettings);
        return extracted.Output._obstacles
            .CombineWith<ICustomDataMapObject>(extracted.Output._notes);
    }

    public static IEnumerable<ModelLetterManager> CreateLetters(Model model, TextSettings settings)
    {
        var letters = model.Objects
            .Where(c => c.Material != null && c.Material.Any(s => s.ToLower().Contains("letter_")))
            .GroupBy(c => Regex.Split(c.Material.Where(s => s.ToLower().Contains("letter_")).First(), "letter_",
                RegexOptions.IgnoreCase).Last());
        var Letters = new List<ModelLetterManager>();
        //Console.WriteLine(letters.Count());
        foreach (var lettercollect in letters)
        {
            var CharVal = Alphabet.nonchar;

            if (!Enum.TryParse(lettercollect.Key, out CharVal))
                throw new ArgumentException(
                    $"Character {lettercollect.Key} is not a member of the character enumerator");


            //scale accordingly
            var cubes = Cube.TransformCollection(new DeltaTransformOptions
            {
                cubes = lettercollect,
                Scale = settings.ImageSettings.scale * scalefactor,
                TransformOrigin = DeltaTransformOptions.TransformOptions.WorldOrigin
            }).ToArray();

            var FullDim = cubes.Select(l => l.Matrix.Value).GetBoundingBox().Main;

            cubes = Cube.TransformCollection(new DeltaTransformOptions
            {
                cubes = cubes,
                Position = new Vector3(-FullDim.Position.X - FullDim.Scale.X / 2f, 0, 0)
            }).ToArray();

            var Dim = new Vector2(FullDim.Scale.X, FullDim.Scale.Y);

            Letters.Add(new ModelLetterManager
            {
                Cubes = cubes.ToArray(),
                Character = (Alphabet)CharVal,
                Dimensions = Dim,
                Settings = settings
            });
        }

        return Letters;
    }
}