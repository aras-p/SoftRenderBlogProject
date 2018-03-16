using Softy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTest
{
    class View
    {
        RenderObject obj;
        Stopwatch timer = new Stopwatch();
        Random random = new Random();
        long lastUpdateTo = 0;
        long updateToLength = 0;
        Vector2 toPosition;
        long lastUpdate = 0;

        public View(Device device, Texture view)
        {
            timer.Start();

            obj = new RenderObject(device);
            obj.Textures.Add(view);

            obj.Shader = ((suv, ouv, obj, wp) =>
            {
                Color result = new Color(0,0,0,255);

                float darkX = Math.Abs(suv.X - 0.5f + (float)Math.Cos(Shaders.Time() / 1000.0f) * 0.1f);
                float darkY = Math.Abs(suv.Y - 0.5f + (float)Math.Cos(Shaders.Time() / 600.0f) * 0.1f);
                float dark = Shaders.Clamp(1 - 4f * (darkX * darkX + darkY * darkY), 0, 1);
                if(dark == 0)
                {
                    return result;
                }

                result = Shaders.SampleTexture(obj.Textures[0], ouv, wp);

                result.B = (byte)(result.B * dark);
                result.G = (byte)(result.G * dark);
                result.R = (byte)(result.R * dark);

                const int dither = 4;
                int ditherB = Math.Min(dither, Math.Min(result.B, 255 - result.B));
                int ditherG = Math.Min(dither, Math.Min(result.G, 255 - result.G));
                int ditherR = Math.Min(dither, Math.Min(result.R, 255 - result.R));

                result.B += Shaders.Dither(ditherB, suv.X);
                result.G += Shaders.Dither(ditherG, suv.X);
                result.R += Shaders.Dither(ditherR, suv.X);

                return result;
            });
        }

        public void Update()
        {
            if(timer.ElapsedMilliseconds > lastUpdateTo + updateToLength)
            {
                toPosition = new Vector2((float)random.NextDouble() - 1, (float)random.NextDouble() - 1);

                updateToLength = random.Next(400, 1500);
                lastUpdateTo = timer.ElapsedMilliseconds;
            }

            obj.Size.X = obj.Size.Y = 2;
            float ratio = ((float)timer.ElapsedMilliseconds - lastUpdateTo) / 10000;
            obj.Position.X = Shaders.Lerp(obj.Position.X, toPosition.X, 0.99f);
            obj.Position.Y = Shaders.Lerp(obj.Position.Y, toPosition.Y, 0.99f);
            lastUpdate = timer.ElapsedMilliseconds;
        }

        public void Draw()
        {
            obj.Draw();
        }
    }
}
