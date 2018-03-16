using System;
using System.Collections.Generic;
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

    public struct Color
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;

        public Color(byte r = 0, byte g = 0, byte b = 0, byte a = 255)
        {
            B = r;
            G = g;
            R = b;
            A = a;
        }

        public static Color DefaultColor = new Color(255, 0, 255, 255);
    }

    public class Texture
    {
        public int Height { get; }
        public int Stride { get; }
        public int Width { get; }

        public int PixelCount => Width * Height;

        public byte[] Data { get; }

        public Texture(byte[] data, int stride)
        {
            Data = data;
            Stride = stride;
            Width = stride / 4;
            Height = Data.Length / Stride;
        }
    }

    public class RenderObject
    {
        Device device;

        public List<Texture> Textures = new List<Texture>();

        public PixelProgram Shader { get; set; }
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

        public Color Sample(Vector2 screenUV, Vector2 objUV)
        {
            if (Shader != null)
            {
                return Shader(screenUV, objUV, this);
            }
            else return Color.DefaultColor;
        }

        public void Draw()
        {
            device.Draw(this);
        }
    }
}
