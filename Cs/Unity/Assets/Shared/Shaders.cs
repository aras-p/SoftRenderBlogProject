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

        public delegate Color PixelProgram(Vector2 screenUV, Vector2 objUV, RenderObject obj);

        public static PixelProgram Textured = (suv, ouv, obj) =>
        {
            return SampleTexture(obj.Textures[0], ouv);
        };

        // Intrisic functions
        static Random random = new Random();
        public static Stopwatch timer = new Stopwatch();
        private static float _time = 0;
        private static float _cosTime1000;
        private static float _cosTime600;

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
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

        public static Color Dither(Color col, Vector2 uv)
        {
            uint hash = IntHash((uint)(uv.x * 70003f + uv.y * 97787f + _time * 17f));
            uint dither = 32;
            byte v = (byte)(hash & (dither - 1));
            if (col.R < 255 - dither) col.R += v;
            if (col.G < 255 - dither) col.G += v;
            if (col.B < 255 - dither) col.B += v;
            return col;
        }

        public static float Lerp(float v1, float v2, float ratio)
        {
            return v1 * ratio + v2 * (1 - ratio);
        }

        public static float Time()
        {
            return _time;
        }
        public static float CosTime1000()
        {
            return _cosTime1000;
        }
        public static float CosTime600()
        {
            return _cosTime600;
        }

        public static void TimeUpdate()
        {
            _time = (float)timer.ElapsedMilliseconds;
            _cosTime1000 = (float)Math.Cos(_time / 1000.0f);
            _cosTime600 = (float)Math.Cos(_time / 600.0f);
        }

        public static Color SampleTexture(Texture texture, Vector2 objUV)
        {
            int coordX = (int)Math.Round((texture.Width - 1) * objUV.x);
            int coordY = (int)Math.Round((texture.Height - 1) * objUV.y);
            int height = coordY * texture.Stride;
            int width = coordX * 4;

            int offs = height + width;
            return new Color(
                texture.Data[offs],
                texture.Data[offs + 1],
                texture.Data[offs + 2],
                texture.Data[offs + 3]);
        }
    }
}
