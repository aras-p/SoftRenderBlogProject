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

        public delegate void PixelProgram(Vector2 screenUV, Vector2 objUV, int cols, float screenUVdx, float objUVdx, Color[] backbuffer, int backbufferOffset);

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
            a = a * 2246822519U;
            a ^= a >> 15;
            a *= 3266489917U;
            a ^= a >> 13;
            return a;
        }

        public static Color Dither(Color col, uint offset)
        {
            uint hash = IntHash(offset) & 31U;
            uint r = Math.Min(col.R + hash, 255);
            uint g = Math.Min(col.G + hash, 255);
            uint b = Math.Min(col.B + hash, 255);
            col.R = (byte)r;
            col.G = (byte)g;
            col.B = (byte)b;
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

        public static int SampleTextureY(Texture texture, Vector2 objUV)
        {
            int coordY = (int)((texture.Height - 1) * objUV.y);
            return coordY * texture.Width;
        }

        public static Color SampleTextureX(Texture texture, int rowOffset, Vector2 objUV)
        {
            int coordX = (int)((texture.Width - 1) * objUV.x);
            return texture.Data[rowOffset + coordX];
        }
    }
}
