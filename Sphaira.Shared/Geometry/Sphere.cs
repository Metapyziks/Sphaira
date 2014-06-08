using OpenTK;

namespace Sphaira.Shared.Geometry
{
    public class Sphere : CelestialBody
    {
        public const float G = 0.00667384f;

        public float Density { get; private set; }

        public float Volume
        {
            get
            {
                return 4f * MathHelper.PiOver3 * Radius * Radius;
            }
        }

        public float Mass
        {
            get
            {
                return Volume * Density;
            }
        }

        public Vector3 Colour
        {
            get;
            set;
        }

        public float Ambient
        {
            get;
            set;
        }

        public float Diffuse
        {
            get;
            set;
        }

        public float Specular
        {
            get;
            set;
        }

        public float Reflect
        {
            get;
            set;
        }

        public Sphere(Vector3 pos, float radius, float density)
            : base(pos, radius)
        {
            Density = density;

            Colour = new Vector3(1f, 1f, 1f);

            Ambient = 0f;
            Diffuse = 0.5f;
            Specular = 0.5f;
            Reflect = 0.5f;
        }

        public float GetGravitationalAcceleration(float altitude)
        {
            var dist = altitude + Radius;
            return G * Mass / (dist * dist);
        }
    }
}
