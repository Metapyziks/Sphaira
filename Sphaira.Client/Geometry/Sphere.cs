using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Utils;
using Sphaira.Client.Graphics;

namespace Sphaira.Client.Geometry
{
    public partial class Sphere : Shared.Geometry.Sphere
    {
        private IndexedVertexBuffer _vb;

        public Sphere(float radius, float density)
            : base(radius, density) { }

        protected override void OnRadiusChanged()
        {
            base.OnRadiusChanged();

            if (_vb == null) {
                _vb = new IndexedVertexBuffer(3);
            }
            
            Vector3[] verts;
            var indices = GenerateVertices(Radius, out verts);

            _vb.SetData(verts);
            _vb.SetIndices(indices);
        }

        public void Render(TestShader shader)
        {
            shader.SetUniform("scale", Radius);
            shader.SetUniform("color", Color4.PaleGoldenrod);

            _vb.Begin(shader);
            _vb.Render();
            _vb.End();
        }
    }
}
