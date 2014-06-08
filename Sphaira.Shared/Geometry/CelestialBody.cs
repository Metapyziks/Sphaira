using System;
using System.Collections.Generic;

using OpenTK;

namespace Sphaira.Shared.Geometry
{
    public abstract class CelestialBody
    {        
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
