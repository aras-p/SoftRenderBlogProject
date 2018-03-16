using System;
using System.Collections.Generic;
using static Softy.Shaders;

namespace Softy
{
    public class Vector2
    {
        private float x, y;

        public float X
        {
            get => x;
            set => x = value;
        }
        public float Y
        {
            get => y;
            set => y = value;
        }

        public int Length => 2;

        public float MagnitudeSqrd
        {
            get
            {
                float result = 0;
                result += (float)Math.Pow(x, 2);
                result += (float)Math.Pow(y, 2);
                return result;
            }
        }

        public float Magnitude => (float)Math.Sqrt(MagnitudeSqrd);

        public Vector2()
        {
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }


        public Vector2 Clamp(float min, float max)
        {
            Vector2 result = new Vector2();
            result.x = x < min ? min : x > max ? max : x;
            result.y = y < min ? min : y > max ? max : y;
            return result;
        }

        public float DistanceFromSqrd(Vector2 a)
        {
            float result = 0;

            Vector2 distanceVector = a - this;
            result += (float)Math.Pow(distanceVector.x, 2);
            result += (float)Math.Pow(distanceVector.y, 2);
            return result;
        }

        public float DistanceFrom(Vector2 a)
        {
            return (float)Math.Sqrt(DistanceFromSqrd(a));
        }

        public float Dot(Vector2 a, int dimensions = 0)
        {
            float result = 0;
            result += x * a.x;
            result += y * a.y;
            return result;
        }

        public static Vector2 operator +(Vector2 a, float b)
        {
            Vector2 result = new Vector2();
            result.x = a.x + b;
            result.y = a.y + b;
            return result;
        }
        public static Vector2 operator -(Vector2 a, float b)
        {
            Vector2 result = new Vector2();
            result.x = (dynamic)a.x - b; //@TODO: dynamic?
            result.y = (dynamic)a.y - b;
            return result;
        }
        public static Vector2 operator *(Vector2 a, float b)
        {
            Vector2 result = new Vector2();
            result.x = a.x * b;
            result.y = a.y * b;
            return result;
        }
        public static Vector2 operator /(Vector2 a, float b)
        {
            Vector2 result = new Vector2();
            result.x = a.x / b;
            result.y = a.y / b;
            return result;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            Vector2 result = new Vector2();
            result.x = a.x + b.x;
            result.y = a.y + b.y;
            return result;
        }
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            Vector2 result = new Vector2();
            result.x = a.x - b.x;
            result.y = a.y - b.y;
            return result;
        }
        public static Vector2 operator *(Vector2 a, Vector2 b)
        {
            Vector2 result = new Vector2();
            result.x = a.x * b.x;
            result.y = a.y * b.y;
            return result;
        }
        public static Vector2 operator /(Vector2 a, Vector2 b)
        {
            Vector2 result = new Vector2();
            result.x = a.x / b.x;
            result.y = a.y / b.y;
            return result;
        }
    }

    public class Matrix
    {
        public float[][] Data;

        public int Length => Data.Length;

        public float[] this[int i]
        {
            get
            {
                return Data[i];
            }

            set
            {
                Data[i] = value;
            }
        }

        public Matrix(int size)
        {
            Data = new float[size][];

            for (int i = 0; i < size; i++)
            {
                Data[i] = new float[size];
            }
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix(m1.Length);

            for (int row = 0; row < result.Data.Length; row++)
            {
                for (int col = 0; col < result[row].Length; col++)
                {
                    for (int i = 0; i < result.Data.Length; i++)
                    {
                        result[row][col] += m1[row][i] * m2[i][col];
                    }
                }
            }

            return result;
        }
    }

    public class Color
    {
        public byte B { get; set; }
        public byte G { get; set; }
        public byte R { get; set; }
        public byte A { get; set; }

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

        //public List<Vector> Vectors = new List<Vector>();
        public List<Matrix> Matrixes = new List<Matrix>();
        public List<Color> Colors = new List<Color>();
        public List<Texture> Textures = new List<Texture>();

        public PixelProgram Shader { get; set; }
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public Vector2 Size { get; set; } = new Vector2(1, 1);

        public int X
        {
            get => (int)(Position.X * device.Width);
            set => Position.X = (float)value / device.Width;
        }
        public int Y
        {
            get => (int)(Position.Y * device.Height);
            set => Position.Y = (float)value / device.Height;
        }
        public int Width
        {
            get => (int)(Size.X * device.Width);
            set => Size.X = (float)value / device.Width;
        }
        public int Height
        {
            get => (int)(Size.Y * device.Height);
            set => Size.Y = (float)value / device.Height;
        }

        public RenderObject(Device device)
        {
            this.device = device;
        }

        public Color Sample(Vector2 screenUV, Vector2 objUV, Color workingPixel)
        {
            if (Shader != null)
            {
                return Shader(screenUV, objUV, this, workingPixel);
            }
            else return Color.DefaultColor;
        }

        public void Draw()
        {
            device.Draw(this);
        }
    }
}
