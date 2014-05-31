using System;
using System.Collections.Generic;

using OpenTK;

using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    abstract class CelestialBody
    {
        public static readonly Comparer<CelestialBody> Comparer =
            Comparer<CelestialBody>.Create((a, b) => {
                var av = a.Position.LengthSquared;
                var bv = b.Position.LengthSquared;

                return av < bv ? -1 : av == bv ? 0 : 1;
            });

        protected static Vector3 GetRandomPosition(Random rand, float near, float far)
        {
            var dist = far - (far - near) * (float) Math.Pow(rand.NextSingle(), 3);

            Vector3 norm;

            do {
                norm = 2f * new Vector3(
                    rand.NextSingle() - .5f,
                    rand.NextSingle() - .5f,
                    rand.NextSingle() - .5f);
            } while (norm.LengthSquared > 1f);

            norm.Normalize();

            return norm.Normalized() * dist;
        }

        public Vector3 Position { get; set; }

        public float Radius { get; set; }

        public CelestialBody()
        {
            Position = Vector3.Zero;
            Radius = 1f;
        }

        public CelestialBody(Vector3 pos, float rad)
        {
            Position = pos;
            Radius = rad;
        }
    }
}
