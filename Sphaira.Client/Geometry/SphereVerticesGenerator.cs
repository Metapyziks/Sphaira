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
        private static int[] GenerateVertices(float quality, out Vector3[] verts)
        {
            float root2 = (float) Math.Sqrt(2f);
            float invRoot2 = 1f / root2;

            var vList = new List<Vector3>() {
                new Vector3( 1f,  0f, -invRoot2).Normalized(),
                new Vector3( 0f,  1f,  invRoot2).Normalized(),
                new Vector3(-1f,  0f, -invRoot2).Normalized(),
                new Vector3( 0f, -1f,  invRoot2).Normalized()
            };

            var iList = new List<int>() {
                0, 1, 2,
                2, 1, 3,
                3, 1, 0,
                0, 2, 3
            };

            for (int i = 0; i < iList.Count; i += 3) {
                var a = iList[i + 0];
                var b = iList[i + 1];
                var c = iList[i + 2];

                var va = vList[a];
                var vb = vList[b];
                var vc = vList[c];

                var area = 0.5f * Vector3.Cross(vc - va, vc - vb).Length * quality;

                if (area > 0.5f) {
                    iList.RemoveRange(i, 3);
                    i -= 3;

                    var vab = (va + vb).Normalized();
                    var vbc = (vb + vc).Normalized();
                    var vac = (va + vc).Normalized();

                    var ab = vList.IndexOf(vab);
                    var bc = vList.IndexOf(vbc);
                    var ac = vList.IndexOf(vac);

                    if (ab == -1) {
                        ab = vList.Count;
                        vList.Add(vab);
                    }
                    if (bc == -1) {
                        bc = vList.Count;
                        vList.Add(vbc);
                    }
                    if (ac == -1) {
                        ac = vList.Count;
                        vList.Add(vac);
                    }

                    iList.Add(a); iList.Add(ab); iList.Add(ac);
                    iList.Add(b); iList.Add(bc); iList.Add(ab);
                    iList.Add(c); iList.Add(ac); iList.Add(bc);
                    iList.Add(ac); iList.Add(ab); iList.Add(bc);
                }
            }

            for (int i = 0; i < iList.Count; i += 3) {
                var a = iList[i + 0];
                var b = iList[i + 1];
                var c = iList[i + 2];

                var va = vList[a];
                var vb = vList[b];
                var vc = vList[c];

                var vab = (va + vb).Normalized();
                var vbc = (vb + vc).Normalized();
                var vac = (va + vc).Normalized();

                var ab = vList.IndexOf(vab);
                var bc = ab == -1 ? vList.IndexOf(vbc) : -1;
                var ac = ab == -1 && bc == -1 ? vList.IndexOf(vac) : -1;

                //if (ab == -1 && bc == -1 && ac == -1) {
                //    var dab = (va - vb).Length;
                //    var dbc = (vb - vc).Length;
                //    var dac = (va - vc).Length;

                //    if (dab > root2 * Math.Max(dbc, dac)) {
                //        ab = vList.Count;
                //        vList.Add(vab);
                //    } else if (dbc > root2 * Math.Max(dab, dac)) {
                //        bc = vList.Count;
                //        vList.Add(vbc);
                //    } else if (dac > root2 * Math.Max(dab, dbc)) {
                //        ac = vList.Count;
                //        vList.Add(vac);
                //    }
                //}

                if (ab != -1 || bc != -1 || ac != -1) {
                    iList.RemoveRange(i, 3);
                    i -= 3;
                }

                if (ab != -1) {
                    iList.Add(a); iList.Add(ab); iList.Add(c);
                    iList.Add(c); iList.Add(ab); iList.Add(b);
                } else if (bc != -1) {
                    iList.Add(a); iList.Add(bc); iList.Add(c);
                    iList.Add(b); iList.Add(bc); iList.Add(a);
                } else if (ac != -1) {
                    iList.Add(b); iList.Add(ac); iList.Add(a);
                    iList.Add(c); iList.Add(ac); iList.Add(b);
                }
            }

            verts = vList.ToArray();

            return iList.ToArray();
        }
    }
}
