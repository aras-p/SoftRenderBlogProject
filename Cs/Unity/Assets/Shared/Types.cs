using System;
using System.Collections.Generic;
using static Softy.Shaders;

namespace Softy
{
    public class Vector
    {
        public float[] Data;

        public float X
        {
            get => Data[0];
            set => Data[0] = value;
        }
        public float Y
        {
            get => Data[1];
            set => Data[1] = value;
        }
        public float Z
        {
            get => Data[2];
            set => Data[2] = value;
        }
        public float W
        {
            get => Data[3];
            set => Data[3] = value;
        }

        public int Length => Data.Length;

        public float MagnitudeSqrd
        {
            get
            {
                float result = 0;

                for (int i = 0; i < Data.Length; i++)
                {
                    result += (float)Math.Pow(Data[i], 2);
                }

                return result;
            }
        }

        public float Magnitude => (float)Math.Sqrt(MagnitudeSqrd);

        public float this[int i]
        {
            get => Data[i];
            set => Data[i] = value;
        }

        public Vector(int i)
        {
            Data = new float[i];
        }

        public Vector(float x, float y) : this(2)
        {
            X = x;
            Y = y;
        }

        public Vector(float x, float y, float z) : this(3)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector(float x, float y, float z, float w) : this(4)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector Clamp(float min, float max)
        {
            Vector result = new Vector(Length);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = Data[i] < min ? min : Data[i] > max ? max : Data[i];
            }

            return result;
        }

        public float DistanceFromSqrd(Vector a)
        {
            float result = 0;

            Vector distanceVector = a - this;

            for (int i = 0; i < MinLength(this, a); i++)
            {
                result += (float)Math.Pow(distanceVector.Data[i], 2);
            }

            return result;
        }

        public float DistanceFrom(Vector a)
        {
            return (float)Math.Sqrt(DistanceFromSqrd(a));
        }

        public float Dot(Vector a, int dimensions = 0)
        {
            float result = 0;
            int d = (dimensions > 0 ? dimensions : Data.Length);

            for (int i = 0; i < d; i++)
            {
                result += Data[i] * a[i];
            }

            return result;
        }

        public override string ToString()
        {
            string result = "Vector(";
            for (int i = 0; i < Length; i++)
            {
                result += Data[i];

                if (i < Length - 1)
                {
                    result += ", ";
                }
            }
            result += ")";

            return result;
        }

        public static Vector operator +(Vector a, float b)
        {
            Vector result = new Vector(a.Length);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = a.Data[i] + b;
            }

            return result;
        }
        public static Vector operator -(Vector a, float b)
        {
            Vector result = new Vector(a.Length);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = (dynamic)a.Data[i] - b;
            }

            return result;
        }
        public static Vector operator *(Vector a, float b)
        {
            Vector result = new Vector(a.Length);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = a.Data[i] * b;
            }

            return result;
        }
        public static Vector operator /(Vector a, float b)
        {
            Vector result = new Vector(a.Length);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = a.Data[i] / b;
            }

            return result;
        }

        public static Vector operator +(Vector a, Vector b)
        {
            Vector result = AllocateResult(a, b);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = a.Data[i] + b.Data[i];
            }

            return result;
        }
        public static Vector operator -(Vector a, Vector b)
        {
            Vector result = AllocateResult(a, b);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = a.Data[i] - b.Data[i];
            }

            return result;
        }
        public static Vector operator *(Vector a, Vector b)
        {
            Vector result = AllocateResult(a, b);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = a.Data[i] * b.Data[i];
            }

            return result;
        }
        public static Vector operator /(Vector a, Vector b)
        {
            Vector result = AllocateResult(a, b);

            for (int i = 0; i < result.Length; i++)
            {
                result.Data[i] = a.Data[i] / b.Data[i];
            }

            return result;
        }

        static int MinLength(Vector a, Vector b)
        {
            return Math.Min(a.Length, b.Length);
        }
        static Vector AllocateResult(Vector a, Vector b)
        {
            int minSize = MinLength(a, b);
            return new Vector(minSize);
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

        public List<Vector> Vectors = new List<Vector>();
        public List<Matrix> Matrixes = new List<Matrix>();
        public List<Color> Colors = new List<Color>();
        public List<Texture> Textures = new List<Texture>();

        public PixelProgram Shader { get; set; }
        public Vector Position { get; set; } = new Vector(0, 0);
        public Vector Size { get; set; } = new Vector(1, 1);

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

        public Color Sample(Vector screenUV, Vector objUV, Color workingPixel)
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
