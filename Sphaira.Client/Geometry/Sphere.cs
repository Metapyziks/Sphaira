using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTKTK.Utils;

namespace Sphaira.Client.Geometry
{
    public partial class Sphere : Shared.Geometry.Sphere
    {
        private VertexBuffer _vb;

        public Sphere(float radius, float density)
            : base(radius, density)
        {
            _vb = new VertexBuffer(3);
        }

        protected override void OnRadiusChanged()
        {
            base.OnRadiusChanged();
            
            var verts = new List<Vector3>();
            var indices = GenerateVertices(3, verts);

            _vb.SetData(verts.ToArray());
        }
    }
}
