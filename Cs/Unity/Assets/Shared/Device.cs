using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Softy
{
    public class Device
    {
        public static readonly int MaxAxisResolution = 4096;

        public int ThreadCount
        {
            get
            {
                return _tasks.Length;
            }

            set
            {
                lock (threadLock)
                {
                    _tasks = new Task[value];
                }
            }
        }
        public bool Checkerboard { get; set; } = false;

        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public int PixelCount { get; }

        public byte[] BackBuffer { get; }
        Queue<RenderObject> renderQueue = new Queue<RenderObject>();

        private Task[] _tasks;
        private object threadLock = new object();

        public static void ReadSettings(string settings, ref int width, ref int height, ref int threadCount, ref bool checkerboard)
        {
            try
            {
                string[] parts = settings.Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                width = int.Parse(parts[0]);
                height = int.Parse(parts[1]);
                threadCount = int.Parse(parts[2]);
                checkerboard = bool.Parse(parts[3]);
            }
            catch (Exception e)
            {
                throw new Exception("Malformed settings file: " + e);
            }
        }


        public Device(int width, int height, int threadCount)
        {
            ThreadCount = threadCount;

            Width = width;
            Height = height;
            Stride = 4 * Width;
            PixelCount = Width * Height;

            BackBuffer = new byte[Stride * height];
            for (int i = 0; i < Stride * height; ++i)
                BackBuffer[i] = 255;
        }

        public void Clear(byte b = 0, byte g = 0, byte r = 0)
        {
            lock (threadLock)
            {
                int pixelsForThread = PixelCount / ThreadCount;

                for (int i = 0; i < ThreadCount; i++)
                {
                    int index = i;
                    _tasks[i] = Task.Factory.StartNew(() => ClearThread(index, pixelsForThread, new Color(b, g, r)));
                }

                for (int i = 0; i < ThreadCount; i++)
                {
                    _tasks[i].Wait();
                }
            }
        }

        public void Draw(RenderObject texture)
        {
            renderQueue.Enqueue(texture);
        }

        public void Render()
        {
            while (renderQueue.Count > 0)
            {
                RenderObject obj = renderQueue.Dequeue();

                lock (threadLock)
                {
                    int strideCount = (int)Math.Ceiling((float)obj.Height / ThreadCount);

                    for (int j = 0; j < ThreadCount; j++)
                    {
                        int index = j;
                        _tasks[j] = Task.Factory.StartNew(() => RenderThread(index, strideCount, obj));
                    }

                    for (int j = 0; j < ThreadCount; j++)
                    {
                        _tasks[j].Wait();
                    }
                }
            }

            renderOdd = 1 - renderOdd;
        }

        void ClearThread(int index, int pixelCount, Color color)
        {
            int startPixel = pixelCount * index;
            int endPixel = index + 1 >= ThreadCount ? BackBuffer.Length / 4 : pixelCount * (index + 1);

            int startOffset = startPixel * 4;
            int endOffset = endPixel * 4;
            for (int i = startOffset; i < endOffset; i += 4)
            {
                BackBuffer[i] = color.B;
                BackBuffer[i + 1] = color.G;
                BackBuffer[i + 2] = color.R;
                BackBuffer[i + 3] = 255;
            }
        }

        int renderOdd = 0;
        void RenderThread(int index, int strideCount, RenderObject obj)
        {
            Color result = new Color(0, 0, 0, 255);

            int startHeight = Shaders.Clamp(obj.Y + strideCount * index, 0, Height);
            int endHeight = Shaders.Clamp(startHeight + strideCount, 0, Height);

            int startWidth = Shaders.Clamp(obj.X, 0, Width);
            int endWidth = Shaders.Clamp(obj.X + obj.Width, 0, Width);

            float invWidth = 1f / Width;
            float invHeight = 1f / Height;
            Vector2 invObjSize = new Vector2(1, 1) / obj.Size;
            int yOffset = startHeight * Stride;
            var checkerboard = Checkerboard;
            var backbuffer = BackBuffer;
            for (int y = startHeight; y < endHeight; y++)
            {
                int offset = yOffset + startWidth * 4;
                for (int x = startWidth; x < endWidth; x++)
                {
                    if (checkerboard && ((x + y) & 1) == renderOdd || !checkerboard)
                    {
                        Vector2 screenUV = new Vector2((float)x * invWidth, (float)y * invHeight);
                        Vector2 objUV = (screenUV - obj.Position) * invObjSize;
                        objUV = objUV.Clamp(0, 1);

                        result = obj.Sample(screenUV, objUV);

                        if (result.A > 0)
                        {
                            backbuffer[offset] = result.B;
                            backbuffer[offset + 1] = result.G;
                            backbuffer[offset + 2] = result.R;
                        }
                    }
                    offset += 4;
                }
                yOffset += Stride;
            }
        }
    }
}
