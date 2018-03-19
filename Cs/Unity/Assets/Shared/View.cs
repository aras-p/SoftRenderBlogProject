using Softy;
using System;
using System.Diagnostics;

namespace PerformanceTest
{
    struct ViewShader : IPixelShader
    {
        public void ExecuteRow(
            Vector2 screenUV, Vector2 objUV,
            int cols, float screenUVdx, float objUVdx,
            Color[] backbuffer, int backbufferOffset,
            ref Texture texture, ref ShaderGlobals globals)
        {
            int texY = Shaders.SampleTextureY(texture, objUV);
            float darkXfac = -0.5f + globals.CosTime1000 * 0.1f;
            screenUV.x += darkXfac;
            float darkY = screenUV.y - 0.5f + globals.CosTime600 * 0.1f;
            float darkY2 = darkY * darkY;
            float darkFac = 1.0f - 4.0f * darkY2;
            uint ditherOffset = (uint)(backbufferOffset + (int)globals.Time);
            for (int x = 0; x < cols; ++x, screenUV.x += screenUVdx, objUV.x += objUVdx, backbufferOffset++, ++ditherOffset)
            {
                Color result = new Color(0xFF000000);

                float darkX = screenUV.x;
                float dark = darkFac - 4f * darkX * darkX;
                if (dark > 0.0f)
                {
                    result = Shaders.SampleTextureX(texture, texY, objUV);

                    uint darkI = (uint)(dark * 255.0f);
                    result.Scale(darkI);

                    result = Shaders.Dither(result, ditherOffset);
                }
                backbuffer[backbufferOffset] = result;
            }
        }
    }

    class View
    {
        public RenderObject obj;
        Stopwatch timer = new Stopwatch();
        Random random = new Random();
        long lastUpdateTo = 0;
        long updateToLength = 0;
        Vector2 toPosition;
        long lastUpdate = 0;

        public View(Device device, Texture view)
        {
            timer.Start();
            obj = new RenderObject(device, view);
            obj.Shader = new ViewShader();
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
    }
}
