using System.Diagnostics;
using System.IO;
using StbSharp;

namespace SoftRenCsCmdline
{
    class Program
    {
        static Softy.Device m_Device;
        static Softy.Texture m_ViewTex;
        static Softy.Texture m_ScopeTex;
        static PerformanceTest.View m_View;
        static PerformanceTest.Scope m_Scope;
        static Stopwatch m_Stopwatch = new Stopwatch();
        static int m_UpdateCounter;

        static void Main(string[] args)
        {
            int width = 640;
            int height = 480;
            int threadCount = 64;
            bool checkerboard = true;
            ReadSettings("Unity/Assets/Data/settings.txt", ref width, ref height, ref threadCount, ref checkerboard);
            m_Device = new Softy.Device(width, height, threadCount)
            {
                Checkerboard = checkerboard
            };

            m_ViewTex = LoadTexture("Unity/Assets/Data/View.png");
            m_ScopeTex = LoadTexture("Unity/Assets/Data/Scope.png");
            m_View = new PerformanceTest.View(m_Device, m_ViewTex);
            m_Scope = new PerformanceTest.Scope(m_Device, m_ScopeTex);

            for (int i = 0; i < 300; ++i)
            {
                UpdateLoop();
                if (m_UpdateCounter == 30)
                {
                    var s = (float)((double)m_Stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) / m_UpdateCounter;
                    System.Console.WriteLine("ms: {0:F2}, FPS: {1:F1}", s * 1000.0f, 1.0f / s);
                    m_UpdateCounter = 0;
                    m_Stopwatch.Reset();
                }
            }
            var colBytes = new byte[m_Device.Width * m_Device.Height * 4];
            for (int i = 0; i < m_Device.BackBuffer.Length; ++i)
            {
                var c = m_Device.BackBuffer[i];
                colBytes[i * 4 + 0] = c.R;
                colBytes[i * 4 + 1] = c.G;
                colBytes[i * 4 + 2] = c.B;
                colBytes[i * 4 + 3] = c.A;
            }

            var image = new Image
            {
                Comp = 4,
                Data = colBytes,
                Width = m_Device.Width,
                Height = m_Device.Height
            };
            var writer = new StbSharp.ImageWriter();
            using (var stream = new MemoryStream())
            {
                writer.WritePng(image, stream);
                File.WriteAllBytes("CmdLineResult.png", stream.ToArray());
            }
        }

        static void UpdateLoop()
        {
            m_Stopwatch.Start();
            m_View.Update();
            m_Scope.Update();

            m_View.Draw();
            m_Scope.Draw();

            m_Device.Render();
            m_Stopwatch.Stop();
            ++m_UpdateCounter;
        }

        public static Softy.Texture LoadTexture(string fileName)
        {
            var bytes = File.ReadAllBytes(fileName);
            var image = StbImage.LoadFromMemory(bytes, StbImage.STBI_rgb_alpha);
            var cols = new Softy.Color[image.Width * image.Height];
            for (int i = 0; i < cols.Length; ++i)
                cols[i] = new Softy.Color(image.Data[i * 4 + 0], image.Data[i * 4 + 1], image.Data[i * 4 + 2], image.Data[i * 4 + 3]);
            return new Softy.Texture(cols, image.Width);
        }

        public static void ReadSettings(string filename, ref int width, ref int height, ref int threadCount, ref bool checkerboard)
        {
            try
            {
                string line = File.ReadAllText(filename);
                Softy.Device.ReadSettings(line, ref width, ref height, ref threadCount, ref checkerboard);
            }
            catch (System.Exception e)
            {
                throw new System.Exception($"Failed to read settings file {filename}: {e}");
            }
        }

    }
}
