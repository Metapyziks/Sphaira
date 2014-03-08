using OpenTK;

namespace Sphaira.Shared.Geometry
{
    public class Sphere
    {
        public const float G = 0.00667384f;

        private float _radius;

        private float _volume;
        private float _mass;

        private bool _volumeChanged;
        private bool _massChanged;

        public Vector3 Position
        {
            get;
            set;
        }

        public float Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                OnRadiusChanged();
            }
        }

        public float Density { get; private set; }

        public float Volume
        {
            get
            {
                if (_volumeChanged) {
                    _volumeChanged = false;

                    _volume = 4f * MathHelper.PiOver3 * Radius * Radius;
                }

                return _volume;
            }
        }

        public float Mass
        {
            get
            {
                if (_massChanged) {
                    _massChanged = false;

                    _mass = Volume * Density;
                }

                return _mass;
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
        {
            Position = pos;

            Radius = radius;
            Density = density;

            OnRadiusChanged();
            OnDensityChanged();

            Colour = new Vector3(1f, 1f, 1f);

            Ambient = 0f;
            Diffuse = 0.5f;
            Specular = 0.75f;
            Reflect = 0.25f;
        }

        public float GetGravitationalAcceleration(float altitude)
        {
            var dist = altitude + Radius;
            return G * Mass / (dist * dist);
        }

        protected virtual void OnRadiusChanged()
        {
            _volumeChanged = true;
            _massChanged = true;
        }

        protected virtual void OnDensityChanged()
        {
            _massChanged = true;
        }
    }
}
