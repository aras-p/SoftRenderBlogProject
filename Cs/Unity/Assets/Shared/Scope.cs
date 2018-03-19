using Softy;
using System;

namespace PerformanceTest
{
    class Scope
    {
        Device device;
        RenderObject obj;
        Texture texture;

        static readonly Vector2 Size = new Vector2(0.8f, 0.8f);
        static readonly Vector2 Position = new Vector2(0.5f, 0.5f);

        public Scope(Device device, Texture scope)
        {
            texture = scope;
            this.device = device;

            obj = new RenderObject(device);

            obj.Shader = ((suv, ouv, cols, screenUVdx, objUVdx, backbuffer, backbufferOffset) =>
            {
                int texY = Shaders.SampleTextureY(texture, ouv);
                for (int x = 0; x < cols; ++x, suv.x += screenUVdx, ouv.x += objUVdx, backbufferOffset++)
                {
                    Color result = Shaders.SampleTextureX(texture, texY, ouv);
                    if (result.A > 0)
                    {
                        backbuffer[backbufferOffset] = result;
                    }
                }
            });
        }

        public void Update()
        {
            obj.Size.x = Size.x * ((float)device.Height / device.Width);
            obj.Size.y = Size.y;

            obj.Position.x = Position.x - obj.Size.x / 2 + (float)Math.Cos(Shaders.Time / 1000.0f) * 0.05f;
            obj.Position.y = Position.y - obj.Size.y / 2 + (float)Math.Cos(Shaders.Time / 600.0f) * 0.05f;
        }

        public void Draw()
        {
            obj.Draw();
        }
    }
}
