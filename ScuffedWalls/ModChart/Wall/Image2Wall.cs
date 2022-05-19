using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ObjEnumerable = System.Collections.Generic.IEnumerable<object>;

namespace ModChart.Wall;

internal class WallImage
{
    private readonly Bitmap _Bitmap;
    private readonly ImageSettings _Settings;

    public WallImage(string path, ImageSettings settings)
    {
        _Settings = settings;
        _Bitmap = new Bitmap(path);
        Run();
    }

    public WallImage(Bitmap image, ImageSettings settings)
    {
        _Settings = settings;
        _Bitmap = image;
        Run();
    }

    public BeatMap.Obstacle[] Walls { get; private set; }


    public void Run()
    {
        var Pixels =
            AnalyzeImage().Values.ToArray()
                .Select(p =>
                {
                    return p
                        .FlipVertical(_Bitmap)
                        .ToFloating()
                        .Resize(_Settings.scale);
                }) //size
                .ToArray();
        // Console.WriteLine(_Settings.scale);

        //centered offseter
        if (_Settings.centered)
            Pixels = Pixels.Select(p =>
                p.Transform(new Vector2 { X = -(_Bitmap.Width.ToFloat() * _Settings.scale / 2), Y = 0 })).ToArray();

        //position offseter
        if (_Settings.Wall._customData["_position"] != null)
            Pixels = Pixels.Select(p => p.Transform(new Vector2
            {
                X = _Settings.Wall._customData.At<ObjEnumerable>("_position")!.ElementAt(1).ToFloat(),
                Y = _Settings.Wall._customData.At<ObjEnumerable>("_position")!.ElementAt(1).ToFloat()
            })).ToArray();

        //color override
        if (_Settings.Wall._customData["_color"] != null)
            Pixels = Pixels.Select(p =>
            {
                p.Color = Color.ColorFromObjArray(_Settings.Wall._customData.At<ObjEnumerable>("_color").ToArray());
                return p;
            }).ToArray();

        var rnd = new Random();

        Walls = Pixels.Select(p =>
        {
            object[] scale;
            TreeDictionary? animatedscale = null;
            if (_Settings.thicc.HasValue)
            {
                p = p.Transform(new Vector2 { X = p.Scale.X / 2f - 1f / _Settings.thicc.Value * 2f, Y = 0 });
                scale = new object[] { 1f / _Settings.thicc, 1f / _Settings.thicc, 1f / _Settings.thicc };
                animatedscale = new TreeDictionary
                {
                    ["_scale"] = new[]
                    {
                        new object[]
                        {
                            p.Scale.X * _Settings.thicc, p.Scale.Y * _Settings.thicc, _Settings.scale * _Settings.thicc,
                            0
                        },
                        new object[]
                        {
                            p.Scale.X * _Settings.thicc, p.Scale.Y * _Settings.thicc, _Settings.scale * _Settings.thicc,
                            1
                        }
                    }
                };
            } //thicc offseter 
            else
            {
                scale = new object[] { p.Scale.X, p.Scale.Y, _Settings.scale };
            }

            var spread = Convert.ToSingle(rnd.Next(-100, 100)) / 100 * _Settings.PCOptimizerPro;

            var wall = new BeatMap.Obstacle
            {
                _time = _Settings.Wall._time.ToFloat() + spread,
                _duration = _Settings.Wall._duration,
                _lineIndex = 0,
                _type = 0,
                _width = 0,
                _customData = new TreeDictionary
                {
                    ["_position"] = new object[] { p.Position.X, p.Position.Y },
                    ["_scale"] = scale,
                    ["_color"] = p.Color.ToObjArray(_Settings.alfa),
                    ["_animation"] = animatedscale
                }
            };
            BeatMap.Append(wall, _Settings.Wall, BeatMap.AppendPriority.Low);

            return wall;
        }).ToArray();
    }

    public string ToDebugString(Dictionary<IntVector2, Pixel> dictionary) //gross
    {
        return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
    }

    private Dictionary<IntVector2, Pixel> AnalyzeImage() //even grosser
    {
        var Pixels = CompressPixels(GetAllPixels(), _Settings.tolerance / _Settings.shift);

        var InverseContainer = new Dictionary<IntVector2, Pixel>();
        foreach (var a in Pixels.Keys) InverseContainer[new IntVector2(a.Y, a.X)] = Pixels[a].Inverse();

        Pixels = CompressPixels(InverseContainer, _Settings.tolerance * _Settings.shift);

        InverseContainer.Clear();
        foreach (var a in Pixels.Keys) InverseContainer[new IntVector2(a.Y, a.X)] = Pixels[a].Inverse();

        return InverseContainer;


        Dictionary<IntVector2, Pixel> GetAllPixels() //truely shame
        {
            var pixels = new Dictionary<IntVector2, Pixel>();
            var Pos = new IntVector2();
            for (Pos.Y = 0; Pos.Y < _Bitmap.Height; Pos.Y++)
            for (Pos.X = 0; Pos.X < _Bitmap.Width; Pos.X++)
                pixels.Add(Pos, _Bitmap.ToPixel(Pos));
            return pixels;
        }

        Dictionary<IntVector2, Pixel> CompressPixels(Dictionary<IntVector2, Pixel> pixels, float tolerance) //just stop
        {
            var Pos = new IntVector2();
            var Dimensions = pixels.Values.ToArray().GetDimensions();
            var CompressedPixels = new Dictionary<IntVector2, Pixel>();

            Pixel? currentPixel = null;
            for (Pos.X = 0; Pos.X < Dimensions.X; Pos.X++)
            for (Pos.Y = 0; Pos.Y < Dimensions.Y; Pos.Y++)
            {
                pixels.TryGetValue(Pos, out var current);
                var exists = pixels.TryGetValue(Pos.Transform(new IntVector2(0, -1)), out var p);
                var countPixel =
                    current != null && //this pixel has to exist
                    current.Equals(p,
                        tolerance) && // the last one has to exist and be the same
                    current.Color.Equals(currentPixel.Color, tolerance) &&
                    !(_Settings.isBlackEmpty && current.Color.isBlackOrEmpty(0.05f)) && //stop immediatly
                    currentPixel.Scale.Y < _Settings.maxPixelLength; //hehe

                if (countPixel) //this vs last, color, width, existance
                {
                    currentPixel.AddHeight();
                }
                else
                {
                    //add and reset
                    if (currentPixel != null && !(_Settings.isBlackEmpty && currentPixel.Color.isBlackOrEmpty(0.05f)))
                        CompressedPixels.Add(currentPixel.Position, currentPixel);

                    currentPixel = current;
                }
            }

            if (currentPixel != null && !(_Settings.isBlackEmpty && currentPixel.Color.isBlackOrEmpty(0.05f)) &&
                !CompressedPixels.ContainsKey(currentPixel.Position))
                CompressedPixels.Add(currentPixel.Position, currentPixel);
            return CompressedPixels;
        }
    }
}

public class ImageSettings
{
    public ImageSettings(bool isBlackEmpty, float scale, float thicc, bool centered, float spread, float alfa,
        BeatMap.Obstacle baseWall)
    {
        this.isBlackEmpty = isBlackEmpty;
        this.scale = scale;
        this.thicc = thicc;
        this.centered = centered;
        PCOptimizerPro = spread;
        this.alfa = alfa;
        Wall = baseWall;
    }

    public ImageSettings()
    {
    }

    public bool isBlackEmpty { get; set; }
    public float scale { get; set; }
    public float? thicc { get; set; }
    public int maxPixelLength { get; set; }
    public bool centered { get; set; }
    public float PCOptimizerPro { get; set; }
    public float alfa { get; set; }
    public float shift { get; set; }
    public float tolerance { get; set; }
    public BeatMap.Obstacle Wall { get; set; }
} //settings

public class Pixel
{
    //data type
    public IntVector2 Position { get; set; }
    public IntVector2 Scale { get; set; }
    public Color Color { get; set; }

    public Pixel FlipVertical(Bitmap bitmap)
    {
        return new Pixel
        {
            Position = new IntVector2 { X = Position.X, Y = bitmap.Height - Position.Y - Scale.Y },
            Scale = Scale,
            Color = Color
        };
    }

    public void AddWidth()
    {
        Scale = Scale.Transform(new IntVector2(1, 0));
    }

    public Pixel Inverse()
    {
        return new Pixel
        {
            Position = new IntVector2 { X = Position.Y, Y = Position.X },
            Scale = new IntVector2 { X = Scale.Y, Y = Scale.X },
            Color = Color
        };
    }

    public void AddHeight()
    {
        Scale = Scale.Transform(new IntVector2 { X = 0, Y = 1 });
    }

    public bool Equals(Pixel? pixel, float tolerance)
    {
        if (pixel == null) return false;
        if (!Color.Equals(pixel.Color, tolerance)) return false;
        return Scale.X == pixel.Scale.X;
    }

    public FloatingPixel ToFloating()
    {
        return new FloatingPixel
        {
            Position = new Vector2 { X = Position.X.ToFloat(), Y = Position.Y.ToFloat() },
            Scale = new Vector2 { X = Scale.X.ToFloat(), Y = Scale.Y.ToFloat() },
            Color = Color
        };
    }
}

public class FloatingPixel
{
    public Vector2 Position { get; set; }
    public Vector2 Scale { get; set; }
    public Color Color { get; set; }

    //0,0 origin
    public FloatingPixel Resize(float factor)
    {
        return new FloatingPixel
        {
            Position = new Vector2
            {
                X = Position.X * factor,
                Y = Position.Y * factor
            },
            Scale = new Vector2
            {
                X = Scale.X * factor,
                Y = Scale.Y * factor
            },
            Color = Color
        };
    }

    public FloatingPixel Transform(Vector2 tranformation)
    {
        return new FloatingPixel
        {
            Position = new Vector2
            {
                X = Position.X + tranformation.X,
                Y = Position.Y + tranformation.Y
            },
            Scale = Scale,
            Color = Color
        };
    }
}

public static class BitmapHelper
{
    public static Bitmap Crop(this Bitmap img, Pixel p)
    {
        //  try
        //  {
        var bmpImage = new Bitmap(img);
        var cropArea = new Rectangle(p.Position.X, p.Position.Y, p.Scale.X, p.Scale.Y);

        return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        //  }
        //  catch
        // {
        //      return null;
        //  }
    }

    public static Pixel? GetCurrent(this Pixel[] pixels, IntVector2 pos)
    {
        if (!pixels.Any(p => p.Position.X == pos.X && p.Position.Y == pos.Y)) return null; //if it dont exist
        //DateTime dateTime = DateTime.Now;
        var thing = pixels.First(p => p.Position.Y == pos.Y && p.Position.X == pos.X);
        //Console.WriteLine("e");
        //Console.WriteLine(DateTime.Now - dateTime);
        return thing;
        //  System.Threading.Tasks.Parallel.
    }

    public static Color GetCurrent(this Bitmap bitmap, IntVector2 pos)
    {
        return new Color
        {
            R = Convert.ToSingle(bitmap.GetPixel(pos.X, pos.Y).R) / 255f,
            G = Convert.ToSingle(bitmap.GetPixel(pos.X, pos.Y).G) / 255f,
            B = Convert.ToSingle(bitmap.GetPixel(pos.X, pos.Y).B) / 255f,
            A = Convert.ToSingle(bitmap.GetPixel(pos.X, pos.Y).A) / 255f
        };
    }

    public static Pixel ToPixel(this Bitmap bitmap, IntVector2 pos)
    {
        return new Pixel
        {
            Position = pos,
            Scale = new IntVector2 { X = 1, Y = 1 },
            Color = bitmap.GetCurrent(pos)
        };
    }

    public static IntVector2 GetDimensions(this Pixel[] pixels)
    {
        if (pixels.Length == 0) return new IntVector2 { X = 0, Y = 0 };
        var SortedY = pixels.OrderBy(p => p.Position.Y).ToArray();
        var SortedX = pixels.OrderBy(p => p.Position.X).ToArray();
        return new IntVector2
        {
            Y = SortedY.Last().Position.Y + SortedY.Last().Scale.Y,
            X = SortedX.Last().Position.X + SortedX.Last().Scale.X
        };
    }

    public static Vector2 GetDimensions(this ICustomDataMapObject[] walls)
    {
        if (walls.Length == 0) return new Vector2 { X = 0, Y = 0 };
        var SortedY = walls.OrderBy(p => p._customData.At<ObjEnumerable>("_position").ElementAt(1).ToFloat()).ToArray();
        var SortedX = walls.OrderBy(p => p._customData.At<ObjEnumerable>("_position").ElementAt(0).ToFloat()).ToArray();
        return new Vector2
        {
            X = SortedX.Last()._customData.At<ObjEnumerable>("_position").ElementAt(0).ToFloat() -
                SortedX.First()._customData.At<ObjEnumerable>("_position").ElementAt(0).ToFloat(),
            Y = SortedY.Last()._customData.At<ObjEnumerable>("_position").ElementAt(1).ToFloat() -
                SortedY.First()._customData.At<ObjEnumerable>("_position").ElementAt(1).ToFloat()
        };
    }

    public static IntVector2 Transform(this IntVector2 t1, IntVector2 t2)
    {
        return new IntVector2
        {
            X = t1.X + t2.X,
            Y = t1.Y + t2.Y
        };
    }

    public static bool IsVerticalBlackOrEmpty(this Bitmap img, IntVector2 pos)
    {
        for (var y = 0; y < img.Height; y++)
            if (!img.GetCurrent(new IntVector2(pos.X, y)).isBlackOrEmpty(0.01f))
                return false;
        return true;
    }
}