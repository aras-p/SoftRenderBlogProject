using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_2018_1_OR_NEWER
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#endif

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
        public int PixelCount { get; }

        public Color[] BackBuffer { get; }
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
            PixelCount = Width * Height;

            BackBuffer = new Color[width * height];
            for (int i = 0; i < width * height; ++i)
                BackBuffer[i] = new Color(0x80808080);
        }

        public void Draw(RenderObject texture)
        {
            renderQueue.Enqueue(texture);
        }

#if UNITY_2018_1_OR_NEWER
        struct RenderJob : IJobParallelFor
        {
            public int strideCount;
            [NativeDisableUnsafePtrRestriction] public IntPtr objPtr;
            [NativeDisableUnsafePtrRestriction] public IntPtr devicePtr;
            public void Execute(int i)
            {
                Device device = ((GCHandle)devicePtr).Target as Device;
                RenderObject obj = ((GCHandle)objPtr).Target as RenderObject;
                device.RenderThread(i, strideCount, obj);
            }
        };
#endif


        public void Render()
        {
            while (renderQueue.Count > 0)
            {
                RenderObject obj = renderQueue.Dequeue();

                lock (threadLock)
                {
                    int strideCount = (int)Math.Ceiling((float)obj.Height / ThreadCount);
#if UNITY_2018_1_OR_NEWER
                    GCHandle handleObj = GCHandle.Alloc(obj, GCHandleType.Pinned);
                    IntPtr ptrObj = (IntPtr)handleObj;
                    GCHandle handleDevice = GCHandle.Alloc(this, GCHandleType.Pinned);
                    IntPtr ptrDevice = (IntPtr)handleDevice;

                    RenderJob jobData;
                    jobData.strideCount = strideCount;
                    jobData.objPtr = ptrObj;
                    jobData.devicePtr = ptrDevice;
                    JobHandle jobHandle = jobData.Schedule(ThreadCount, 1);
                    jobHandle.Complete();

                    handleObj.Free();
                    handleDevice.Free();
#else

                    for (int j = 0; j < ThreadCount; j++)
                    {
                        int index = j;
                        _tasks[j] = Task.Factory.StartNew(() => RenderThread(index, strideCount, obj));
                    }

                    for (int j = 0; j < ThreadCount; j++)
                    {
                        _tasks[j].Wait();
                    }
#endif
                }
            }

            renderOdd = 1 - renderOdd;
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
            Vector2 objPos = obj.Position;
            Vector2 invObjSize = new Vector2(1, 1) / obj.Size;
            int yOffset = startHeight * Width;
            var checkerboard = Checkerboard;
            var backbuffer = BackBuffer;
            for (int y = startHeight; y < endHeight; y++, yOffset += Width)
            {
                if (checkerboard && ((y % 2) != renderOdd))
                    continue;
                int offset = yOffset + startWidth;
                Vector2 screenUV = new Vector2((float)startWidth * invWidth, (float)y * invHeight);
                Vector2 objUV = (screenUV - objPos) * invObjSize;
                objUV.y = Shaders.Clamp(objUV.y, 0f, 1f);
                obj.Shader.ExecuteRow(screenUV, objUV, endWidth - startWidth, invWidth, invWidth*invObjSize.x, backbuffer, offset, ref obj.texture, ref Shaders.globals);
            }
        }
    }
}
