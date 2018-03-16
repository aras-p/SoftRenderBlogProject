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

        static readonly Vector Size = new Vector(0.8f, 0.8f);
        static readonly Vector Position = new Vector(0.5f, 0.5f);

        public Scope(Device device, Texture scope)
        {
            this.device = device;

            obj = new RenderObject(device);
            obj.Textures.Add(scope);
            obj.Vectors.Add(new Vector(Position.X, Position.Y));

            obj.Shader = ((suv, ouv, obj, wp) =>
            {
                Color result = Shaders.SampleTexture(obj.Textures[0], ouv, wp);

                return result;
            });
        }

        public void Update()
        {
            obj.Size.X = Size.X * ((float)device.Height / device.Width);
            obj.Size.Y = Size.Y;

            obj.Position.X = Position.X - obj.Size.X / 2 + (float)Math.Cos(Shaders.Time() / 1000.0f) * 0.05f;
            obj.Position.Y = Position.Y - obj.Size.Y / 2 + (float)Math.Cos(Shaders.Time() / 600.0f) * 0.05f;
        }

        public void Draw()
        {
            obj.Draw();
        }
    }
}
