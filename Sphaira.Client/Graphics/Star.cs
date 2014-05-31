using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;

using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    class Star : CelestialBody
    {
        public static Star[] Generate(Random rand, int count)
        {
            float minRad = 1f / 16f;
            float maxRad = 1f / 2f;

            float near = 8f;
            float far = 32f;

            var stars = new Star[count];

            for (int i = 0; i < count; ++i) {
                float shift = rand.NextSingle() * 2f - 1f;

                float rad = minRad + (float) Math.Pow(rand.NextDouble(), 16) * (maxRad - minRad);

                var pos = GetRandomPosition(rand, near, far);
                var clr = new Color4(0.9f + shift * 0.1f, 0.9f - Math.Abs(shift) * 0.2f, 0.85f - shift * 0.1f, 1f);
                var star = new Star(pos, rad, clr);

                stars[i] = star;
            }

            Array.Sort(stars, Comparer);

            return stars;
        }

        public Color4 Color { get; set; }

        public Star(Vector3 pos, float rad, Color4 clr)
            : base(pos, rad)
        {
            Color = clr;
        }
    }
}
