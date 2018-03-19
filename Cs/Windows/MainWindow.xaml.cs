using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Softy;
using System.Diagnostics;
using PerformanceTest;

namespace SoftRenTest
{
    public partial class MainWindow : Window
    {
        WriteableBitmap FrontBuffer;
        byte[] FrontBytes;
        Stopwatch stopwatch = new Stopwatch();
        int counter;
        Device device;
        Thread mainLoop;

        View view;
        Scope scope;

        public MainWindow()
        {
            InitializeComponent();

            int width = 320;
            int height = 240;
            int threadCount = 4;
            bool checkerboard = false;
            ReadSettings("Unity/Assets/Data/settings.txt", ref width, ref height, ref threadCount, ref checkerboard);

            device = new Device(width, height, threadCount);
            device.Checkerboard = checkerboard;
            FrontBuffer = new WriteableBitmap(device.Width, device.Height, 96, 96, PixelFormats.Bgra32, null);
            FrontBytes = new byte[device.Width * device.Height * 4];
            Viewport.Source = FrontBuffer;

            view = new View(device, LoadTexture("Unity/Assets/Data/View.png"));
            scope = new Scope(device, LoadTexture("Unity/Assets/Data/Scope.png"));

            mainLoop = new Thread(MainLoop)
            {
                IsBackground = true
            };
            mainLoop.Start();
        }

        void MainLoop()
        {
            while (true)
            {
                stopwatch.Start();
                view.Update();
                scope.Update();

                device.Draw(view.obj);
                device.Draw(scope.obj);

                device.Render();
                stopwatch.Stop();
                ++counter;
                UpdateMainWindow();
            }
        }

        public void UpdateMainWindow()
        {
            var s = 0.0f;
            if (counter == 80)
            {
                s = (float)((double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) / counter;
                stopwatch.Reset();
                counter = 0;
            }
            Dispatcher.Invoke(() =>
            {
                if (s != 0)
                    FPS.Content = string.Format("ms: {0:F2}, FPS: {1:F1}", s * 1000.0f, 1.0f / s);

                for (int i = 0; i < device.BackBuffer.Length; ++i)
                {
                    var c = device.BackBuffer[i];
                    FrontBytes[i * 4 + 0] = c.B;
                    FrontBytes[i * 4 + 1] = c.G;
                    FrontBytes[i * 4 + 2] = c.R;
                    FrontBytes[i * 4 + 3] = c.A;
                }
                FrontBuffer.Lock();
                FrontBuffer.WritePixels(
                    new Int32Rect(0, 0, device.Width, device.Height),
                    FrontBytes,
                    device.Width*4, 0);
                FrontBuffer.Unlock();
            });
        }

        public static Texture LoadTexture(string fileName)
        {
            BitmapSource bitmapSource = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));
            FormatConvertedBitmap newBitmapSource = new FormatConvertedBitmap();
            newBitmapSource.BeginInit();
            newBitmapSource.Source = bitmapSource;
            newBitmapSource.DestinationFormat = PixelFormats.Bgra32;
            newBitmapSource.EndInit();
            byte[] rawBytes = new byte[newBitmapSource.PixelWidth * newBitmapSource.PixelHeight * 4];
            newBitmapSource.CopyPixels(rawBytes, newBitmapSource.PixelWidth*4, 0);
            Softy.Color[] rawImage = new Softy.Color[newBitmapSource.PixelWidth * newBitmapSource.PixelHeight];
            for (int i = 0; i < newBitmapSource.PixelWidth * newBitmapSource.PixelHeight; ++i)
                rawImage[i] = new Softy.Color(rawBytes[i * 4 + 2], rawBytes[i * 4 + 1], rawBytes[i * 4 + 0], rawBytes[i * 4 + 3]);
            Texture texture = new Texture(rawImage, newBitmapSource.PixelWidth);
            return texture;
        }

        public static void ReadSettings(string filename, ref int width, ref int height, ref int threadCount, ref bool checkerboard)
        {
            try
            {
                string line = File.ReadAllText(filename);
                Softy.Device.ReadSettings(line, ref width, ref height, ref threadCount, ref checkerboard);
            }
            catch (Exception e)
            {
                throw new Exception($"No settings file {filename}: {e}");
            }
        }
    }
}
