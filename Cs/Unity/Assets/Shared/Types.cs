using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Softy.Shaders;

namespace Softy
{
    public struct Vector2
    {
        public float x, y;

        public int Length => 2;

        public float MagnitudeSqrd
        {
            get
            {
                return x * x + y * y;
            }
        }

        public float Magnitude => (float)Math.Sqrt(MagnitudeSqrd);

        public Vector2(float _x, float _y)
        {
            x = _x;
            y = _y;
        }


        public Vector2 Clamp(float min, float max)
        {
            return new Vector2(
                x < min ? min : x > max ? max : x,
                y < min ? min : y > max ? max : y);
        }

        public float DistanceFromSqrd(Vector2 a)
        {
            float result = 0;

            Vector2 distanceVector = a - this;
            result += distanceVector.x * distanceVector.x;
            result += distanceVector.y * distanceVector.y;
            return result;
        }

        public float DistanceFrom(Vector2 a)
        {
            return (float)Math.Sqrt(DistanceFromSqrd(a));
        }

        public float Dot(Vector2 a, int dimensions = 0)
        {
            return x * a.x + y * a.y;
        }

        public static Vector2 operator +(Vector2 a, float b)
        {
            return new Vector2
            {
                x = a.x + b,
                y = a.y + b
            };
        }
        public static Vector2 operator -(Vector2 a, float b)
        {
            return new Vector2
            {
                x = a.x - b,
                y = a.y - b
            };
        }
        public static Vector2 operator *(Vector2 a, float b)
        {
            return new Vector2
            {
                x = a.x * b,
                y = a.y * b
            };
        }
        public static Vector2 operator /(Vector2 a, float b)
        {
            return new Vector2
            {
                x = a.x / b,
                y = a.y / b
            };
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2
            {
                x = a.x + b.x,
                y = a.y + b.y
            };
        }
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2
            {
                x = a.x - b.x,
                y = a.y - b.y
            };
        }
        public static Vector2 operator *(Vector2 a, Vector2 b)
        {
            return new Vector2
            {
                x = a.x * b.x,
                y = a.y * b.y
            };
        }
        public static Vector2 operator /(Vector2 a, Vector2 b)
        {
            return new Vector2
            {
                x = a.x / b.x,
                y = a.y / b.y
            };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Color
    {
        [FieldOffset(0)] public byte R;
        [FieldOffset(1)] public byte G;
        [FieldOffset(2)] public byte B;
        [FieldOffset(3)] public byte A;
        [FieldOffset(0)] public uint RGBA;

        public Color(byte r = 0, byte g = 0, byte b = 0, byte a = 255)
        {
            RGBA = 0;
            R = r;
            G = g;
            B = b;
            A = a;
        }
        public Color(uint rgba)
        {
            B = 0;
            G = 0;
            R = 0;
            A = 0;
            RGBA = rgba;
        }
    }

    public class Texture
    {
        public int Height;
        public int Width;
        public Color[] Data;

        public Texture(Color[] data, int width)
        {
            Data = data;
            Width = width;
            Height = Data.Length / width;
        }
    }

    public class RenderObject
    {
        Device device;

        public PixelProgram Shader;
        public Vector2 Position = new Vector2(0, 0);
        public Vector2 Size = new Vector2(1, 1);

        public int X
        {
            get => (int)(Position.x * device.Width);
            set => Position.x = (float)value / device.Width;
        }
        public int Y
        {
            get => (int)(Position.y * device.Height);
            set => Position.y = (float)value / device.Height;
        }
        public int Width
        {
            get => (int)(Size.x * device.Width);
            set => Size.x = (float)value / device.Width;
        }
        public int Height
        {
            get => (int)(Size.y * device.Height);
            set => Size.y = (float)value / device.Height;
        }

        public RenderObject(Device device)
        {
            this.device = device;
        }

        public void Draw()
        {
            device.Draw(this);
        }
    }
}
