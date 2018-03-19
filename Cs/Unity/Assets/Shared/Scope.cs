using Softy;
using System;

namespace PerformanceTest
{
    struct ScopeShader : IPixelShader
    {
        public void ExecuteRow(
            Vector2 screenUV, Vector2 objUV,
            int cols, float screenUVdx, float objUVdx,
            Color[] backbuffer, int backbufferOffset,
            ref Texture texture, ref ShaderGlobals globals)
        {
            int texY = Shaders.SampleTextureY(texture, objUV);
            for (int x = 0; x < cols; ++x, objUV.x += objUVdx, backbufferOffset++)
            {
                Color result = Shaders.SampleTextureX(texture, texY, objUV);
                if (result.A > 0)
                {
                    backbuffer[backbufferOffset] = result;
                }
            }
        }
    }

    class Scope
    {
        public RenderObject obj;

        static readonly Vector2 Size = new Vector2(0.8f, 0.8f);
        static readonly Vector2 Position = new Vector2(0.5f, 0.5f);

        public Scope(Device device, Texture scope)
        {
            obj = new RenderObject(device, scope);
            obj.Shader = new ScopeShader();
        }

        public void Update()
        {
            obj.Size.x = Size.x * ((float)obj.device.Height / obj.device.Width);
            obj.Size.y = Size.y;

            obj.Position.x = Position.x - obj.Size.x / 2 + (float)Math.Cos(Shaders.globals.Time / 1000.0f) * 0.05f;
            obj.Position.y = Position.y - obj.Size.y / 2 + (float)Math.Cos(Shaders.globals.Time / 600.0f) * 0.05f;
        }
    }
}
