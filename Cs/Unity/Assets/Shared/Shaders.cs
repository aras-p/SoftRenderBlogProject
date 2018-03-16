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

        // Intrisic functions
        static Random random = new Random();
        public static Stopwatch timer = new Stopwatch();
        private static int _time = 0;

        public static T Clamp<T>(T value, T min, T max) where T : IComparable
        {
            return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
        }

        static private uint IntHash(uint a)
        {
            a = (a + 0x7ed55d16) + (a << 12);
            a = (a ^ 0xc761c23c) ^ (a >> 19);
            a = (a + 0x165667b1) + (a << 5);
            a = (a + 0xd3a2646c) ^ (a << 9);
            a = (a + 0xfd7046c5) + (a << 3);
            a = (a ^ 0xb55a4f09) ^ (a >> 16);
            return a;
        }

        public static byte Dither(int strength, Vector2 salt)
        {
            if (strength < 1)
                strength = 1;
            uint h = IntHash((uint)(salt.x * 70003 + salt.y * 97787 + _time * 17));
            return (byte)(h % strength);
        }

        public static float Lerp(float v1, float v2, float ratio)
        {
            return v1 * ratio + v2 * (1 - ratio);
        }

        public static int Time()
        {
            return _time;
        }

        public static void TimeUpdate()
        {
            _time = (int)timer.ElapsedMilliseconds;
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
