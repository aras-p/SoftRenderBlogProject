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
            Viewport.Source = FrontBuffer;

            view = new View(device, LoadTexture("Unity/Assets/Data/View.png"));
            scope = new Scope(device, LoadTexture("Unity/Assets/Data/Scope.png"));

            mainLoop = new Thread(MainLoop)
            {
                IsBackground = true
            };
            mainLoop.Start();
            device.Clear(127);
        }

        void MainLoop()
        {
            while (true)
            {
                stopwatch.Start();
                view.Update();
                scope.Update();

                view.Draw();
                scope.Draw();

                device.Render();
                stopwatch.Stop();
                ++counter;
                UpdateMainWindow();
            }
        }

        public void UpdateMainWindow()
        {
            var s = 0.0f;
            if (counter == 30)
            {
                s = (float)((double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) / counter;
                stopwatch.Reset();
                counter = 0;
            }
            Dispatcher.Invoke(() =>
            {
                if (s != 0)
                    FPS.Content = string.Format("ms: {0:F2}, FPS: {1:F1}", s * 1000.0f, 1.0f / s);

                FrontBuffer.Lock();
                FrontBuffer.WritePixels(
                    new Int32Rect(0, 0, device.Width, device.Height),
                    device.BackBuffer,
                    device.Stride, 0);
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
            int stride = (newBitmapSource.PixelWidth * newBitmapSource.Format.BitsPerPixel / 8);
            byte[] rawImage = new byte[(stride * newBitmapSource.PixelHeight)];
            newBitmapSource.CopyPixels(rawImage, stride, 0);
            Texture texture = new Texture(rawImage, stride);
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
