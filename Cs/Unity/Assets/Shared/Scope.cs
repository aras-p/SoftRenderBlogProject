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

            obj.Shader = ((suv, ouv, obj) =>
            {
                Color result = Shaders.SampleTexture(obj.Textures[0], ouv);

                return result;
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
