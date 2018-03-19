﻿using Softy;
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
        long lastUpdate = 0;
        Texture texture;

        public View(Device device, Texture view)
        {
            texture = view;
            timer.Start();

            obj = new RenderObject(device);

            obj.Shader = ((suv, ouv, cols, screenUVdx, objUVdx, backbuffer, backbufferOffset) =>
            {
                int texY = Shaders.SampleTextureY(texture, ouv);
                float darkXfac = -0.5f + Shaders.CosTime1000 * 0.1f;
                suv.x += darkXfac;
                float darkY = Math.Abs(suv.y - 0.5f + Shaders.CosTime600 * 0.1f);
                float darkY2 = darkY * darkY;
                float darkFac = 1.0f - 4.0f * darkY2;
                uint ditherOffset = (uint)(backbufferOffset + (int)Shaders.Time);
                for (int x = 0; x < cols; ++x, suv.x += screenUVdx, ouv.x += objUVdx, backbufferOffset++, ++ditherOffset)
                {
                    Color result = new Color(0xFF000000);

                    float darkX = suv.x;
                    float dark = darkFac - 4f * darkX * darkX;
                    if (dark > 0.0f)
                    {
                        result = Shaders.SampleTextureX(texture, texY, ouv);

                        uint darkI = (uint)(dark * 255.0f);
                        result.Scale(darkI);

                        result = Shaders.Dither(result, ditherOffset);
                    }
                    backbuffer[backbufferOffset] = result;
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
