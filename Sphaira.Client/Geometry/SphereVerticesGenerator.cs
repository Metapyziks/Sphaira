using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Sphaira.Client.Geometry
{
    public partial class Sphere : Shared.Geometry.Sphere
    {
        private static int[] GenerateVertices(int power, List<Vector3> verts)
        {
            if (power == 0) {
                float invRoot2 = 1f / (float) Math.Sqrt(2f);

                verts.Clear();
                verts.Add(new Vector3( 1f,  0f, -invRoot2).Normalized());
                verts.Add(new Vector3( 0f,  1f,  invRoot2).Normalized());
                verts.Add(new Vector3(-1f,  0f, -invRoot2).Normalized());
                verts.Add(new Vector3( 0f, -1f,  invRoot2).Normalized());

                return new[] {
                    0, 1, 2,
                    2, 1, 3,
                    3, 1, 0,
                    0, 2, 3
                };
            }

            var oldIndices = GenerateVertices(power - 1, verts);
            var newIndices = new int[oldIndices.Length * 3];

            for (int i = 0, j = 0; i < oldIndices.Length; i += 3) {
                var a = oldIndices[i + 0];
                var b = oldIndices[i + 1];
                var c = oldIndices[i + 2];
                var d = verts.Count;

                verts.Add((verts[a] + verts[b] + verts[c]).Normalized());

                newIndices[j++] = a; newIndices[j++] = b; newIndices[j++] = d;
                newIndices[j++] = b; newIndices[j++] = c; newIndices[j++] = d;
                newIndices[j++] = c; newIndices[j++] = a; newIndices[j++] = d;
            }

            return newIndices;
        }
    }
}
