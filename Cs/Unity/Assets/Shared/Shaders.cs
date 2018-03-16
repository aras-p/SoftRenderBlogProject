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
        public static float Time = 0;
        public static float CosTime1000;
        public static float CosTime600;

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
            uint dither = 32;
            uint hash = IntHash((uint)(uv.x * 70003f + uv.y * 97787f + Time * 17f));
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

        public static void TimeUpdate()
        {
            Time = (float)timer.ElapsedMilliseconds;
            CosTime1000 = (float)Math.Cos(Time / 1000.0f);
            CosTime600 = (float)Math.Cos(Time / 600.0f);
        }

        public static Color SampleTexture(Texture texture, Vector2 objUV)
        {
            int coordX = (int)((texture.Width - 1) * objUV.x);
            int coordY = (int)((texture.Height - 1) * objUV.y);
            int height = coordY * texture.Stride;
            int width = coordX * 4;

            int offs = height + width;
            var data = texture.Data;
            return new Color(
                data[offs],
                data[offs + 1],
                data[offs + 2],
                data[offs + 3]);
        }
    }
}
