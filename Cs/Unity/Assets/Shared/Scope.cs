using Softy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTest
{
    class Scope
    {
        Device device;
        RenderObject obj;

        static readonly Vector2 Size = new Vector2(0.8f, 0.8f);
        static readonly Vector2 Position = new Vector2(0.5f, 0.5f);

        public Scope(Device device, Texture scope)
        {
            this.device = device;

            obj = new RenderObject(device);
            obj.Textures.Add(scope);

            obj.Shader = ((suv, ouv, obj, cols, screenUVdx, objUVdx, backbuffer, backbufferOffset) =>
            {
                int texY = Shaders.SampleTextureY(obj.Textures[0], ouv);
                for (int x = 0; x < cols; ++x, suv.x += screenUVdx, ouv.x += objUVdx, backbufferOffset += 4)
                {
                    Color result = Shaders.SampleTextureX(obj.Textures[0], texY, ouv);
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
