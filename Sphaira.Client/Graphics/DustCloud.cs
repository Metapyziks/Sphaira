using System;
using System.Linq;

using OpenTK;
using OpenTK.Graphics;

namespace Sphaira.Client.Graphics
{
    class DustCloud : CelestialBody
    {
        public static DustCloud[] Generate(Random rand, int count)
        {
            float minRad = 1f;
            float maxRad = 16f;

            float near = 20f;
            float far = 32f;

            var clrPairs = new Color4[,] {
                { Color4.Red, Color4.Blue }
            };

            int variation = rand.Next(16, 32);

            var seta = new Vector3[variation];
            var setb = new Vector3[variation];
            for (int i = 0; i < variation; ++i) {
                seta[i] = GetRandomPosition(rand, near, far);
                setb[i] = GetRandomPosition(rand, near, far);
            }

            int index = rand.Next(clrPairs.GetLength(0));
            var clra = clrPairs[index, 0];
            var clrb = clrPairs[index, 1];

            var dusts = new DustCloud[count];

            for (int i = 0; i < count; ++i) {
                var pos = GetRandomPosition(rand, near, far);

                float scale = (float) Math.Pow(rand.NextDouble(), 4);

                float rad = minRad + scale * (maxRad - minRad);
                float alpha = (1 - scale) / 96f;

                var suma = seta.Sum(x => 1f / Math.Max(0.125f, (pos - x).LengthSquared));
                var sumb = setb.Sum(x => 1f / Math.Max(0.125f, (pos - x).LengthSquared));

                var shift = (suma * suma) / ((suma * suma) + (sumb * sumb));

                var clr = new Color4(
                    clra.R * shift + clrb.R * (1f - shift),
                    clra.G * shift + clrb.G * (1f - shift),
                    clra.B * shift + clrb.B * (1f - shift),
                    alpha);

                var dust = new DustCloud(pos, rad, clr);

                dusts[i] = dust;
            }

            Array.Sort(dusts, Comparer);

            return dusts;
        }

        public Color4 Color { get; set; }

        public DustCloud(Vector3 pos, float rad, Color4 clr)
            : base(pos, rad)
        {
            Color = clr;
        }
    }
}
