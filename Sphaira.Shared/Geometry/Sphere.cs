using OpenTK;

namespace Sphaira.Shared.Geometry
{
    public abstract class Sphere
    {
        private float _volume;
        private float _mass;

        private bool _volumeChanged;
        private bool _massChanged;

        public float Radius { get; private set; }

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

        protected Sphere(float radius, float density)
        {
            Radius = radius;
            Density = density;

            OnRadiusChanged();
            OnDensityChanged();
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
