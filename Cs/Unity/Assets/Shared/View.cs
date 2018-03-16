using Softy;
using System;
using System.Diagnostics;

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
        //Vector2 toZoom;
        long lastUpdate = 0;

        public View(Device device, Texture view)
        {
            timer.Start();

            obj = new RenderObject(device);
            obj.Textures.Add(view);

            obj.Shader = ((suv, ouv, obj, cols, screenUVdx, objUVdx, backbuffer, backbufferOffset) =>
            {
                for (int x = 0; x < cols; ++x, suv.x += screenUVdx, ouv.x += objUVdx, backbufferOffset += 4)
                {
                    Color result = new Color(0, 0, 0, 255);

                    float darkX = Math.Abs(suv.x - 0.5f + Shaders.CosTime1000 * 0.1f);
                    float darkY = Math.Abs(suv.y - 0.5f + Shaders.CosTime600 * 0.1f);
                    float dark = Shaders.Clamp(1 - 4f * (darkX * darkX + darkY * darkY), 0, 1);
                    if (dark != 0)
                    {
                        result = Shaders.SampleTexture(obj.Textures[0], ouv);

                        result.B = (byte)(result.B * dark);
                        result.G = (byte)(result.G * dark);
                        result.R = (byte)(result.R * dark);
                        result = Shaders.Dither(result, suv);
                    }
                    if (result.A > 0)
                    {
                        backbuffer[backbufferOffset] = result.B;
                        backbuffer[backbufferOffset+1] = result.G;
                        backbuffer[backbufferOffset+2] = result.R;
                    }
                }
            });
        }

        public void Update()
        {
            Shaders.TimeUpdate();
            if (timer.ElapsedMilliseconds > lastUpdateTo + updateToLength)
            {
                toPosition = new Vector2((float)random.NextDouble() - 1, (float)random.NextDouble() - 1);

                updateToLength = random.Next(400, 1500);
                lastUpdateTo = timer.ElapsedMilliseconds;
            }

            obj.Size.x = obj.Size.y = 2;
            float ratio = ((float)timer.ElapsedMilliseconds - lastUpdateTo) / 10000;
            obj.Position.x = Shaders.Lerp(obj.Position.x, toPosition.x, 0.99f);
            obj.Position.y = Shaders.Lerp(obj.Position.y, toPosition.y, 0.99f);
            lastUpdate = timer.ElapsedMilliseconds;
        }

        public void Draw()
        {
            obj.Draw();
        }
    }
}
