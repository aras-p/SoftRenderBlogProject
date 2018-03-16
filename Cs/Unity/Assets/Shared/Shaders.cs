using System;
using System.Diagnostics;

namespace Softy
{
    public static class Shaders
    {
        static Shaders()
        {
            timer.Start();
        }

        public delegate Color PixelProgram(Vector2 screenUV, Vector2 objUV, RenderObject obj, Color workingPixel);

        public static PixelProgram Colored = (suv, ouv, obj, wp) =>
        {
            return obj.Colors[0];
        };

        public static PixelProgram Textured = (suv, ouv, obj, wp) =>
        {
            return SampleTexture(obj.Textures[0], ouv, wp);
        };

        // Intrinsic functions
        static Random random = new Random();
        public static Stopwatch timer = new Stopwatch();

        public static T Clamp<T>(T value, T min, T max) where T : IComparable
        {
            return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
        }

        public static byte Dither(int strength, float salt)
        {
            return (byte)((3.1415 * salt * timer.ElapsedTicks % (strength * 2 + 1)) - strength);
        }

        public static float Lerp(float v1, float v2, float ratio)
        {
            return v1 * ratio + v2 * (1 - ratio);
        }

        public static int Time()
        {
            return (int)timer.ElapsedMilliseconds;
        }
        
        public static Color SampleTexture(Texture texture, Vector2 objUV, Color result)
        {
            int coordX = (int)Math.Round((texture.Width - 1) * objUV.x);
            int coordY = (int)Math.Round((texture.Height - 1) * objUV.y);
            int height = coordY * texture.Stride;
            int width = coordX * 4;

            result.B = texture.Data[height + width];
            result.G = texture.Data[height + width + 1];
            result.R = texture.Data[height + width + 2];
            result.A = texture.Data[height + width + 3];

            return result;
        }
    }
}
