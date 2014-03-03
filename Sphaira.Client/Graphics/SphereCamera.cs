using System;

using OpenTK;

using OpenTKTK.Scene;

using Sphaira.Shared.Geometry;

namespace Sphaira.Client.Graphics
{
    public class SphereCamera : Camera
    {
        private Quaternion _sphereRot;

        public Sphere Sphere { get; private set; }

        public float Altitude { get; set; }

        public float EyeHeight { get; private set; }

        private Vector3 _velocity;

        public override Vector3 Position
        {
            get { return Vector3.Transform((Altitude + Sphere.Radius) * Vector3.UnitY, _sphereRot.Inverted()); }
            set { throw new NotImplementedException(); }
        }

        public override float X
        {
            get { return Position.X; }
            set { throw new NotImplementedException(); }
        }

        public override float Y
        {
            get { return Position.Y; }
            set { throw new NotImplementedException(); }
        }

        public override float Z
        {
            get { return Position.Z; }
            set { throw new NotImplementedException(); }
        }

        public SphereCamera(int width, int height, Sphere sphere, float eyeHeight)
            : base(width, height, MathHelper.PiOver3, 1f / 8f, 65536f)
        {
            Sphere = sphere;
            EyeHeight = eyeHeight;
            Altitude = EyeHeight;

            _sphereRot = new Quaternion(Vector3.UnitY, 0f);
            _velocity = Vector3.Zero;
        }

        protected override void OnUpdateViewMatrix(ref Matrix4 matrix)
        {
            var yRot = Matrix4.CreateRotationY(Yaw);
            var xRot = Matrix4.CreateRotationX(Pitch);
            var trns = Matrix4.CreateTranslation(-Vector3.UnitY * (Sphere.Radius + Altitude));
            var sRot = Matrix4.CreateFromQuaternion(_sphereRot);

            // Combine the matrices to find the view transformation
            matrix = Matrix4.Mult(sRot, Matrix4.Mult(trns, Matrix4.Mult(yRot, xRot)));
        }

        public void Push(Vector2 vec)
        {
            _velocity.X += vec.X;
            _velocity.Z += vec.Y;
        }

        public void Jump(float vel)
        {
            _velocity.Y += vel;
        }

        public void UpdateFrame(FrameEventArgs e)
        {
            var vel = _velocity.Xz * (float) e.Time;

            if (vel.LengthSquared > 0f) {
                var vec3 = new Vector3(vel.X, 0f, vel.Y).Normalized();
                var norm = new Vector3(0f, 1f, 0f);

                var axis = Vector3.Cross(norm, -vec3);
                var arc = vel.Length / Sphere.Radius;

                _sphereRot = Quaternion.Multiply(Quaternion.FromAxisAngle(axis, arc), _sphereRot);

                if (Altitude <= EyeHeight) {
                    _velocity.X *= 0.8f;
                    _velocity.Z *= 0.8f;
                }
            }

            if (Altitude > EyeHeight || _velocity.Y > 0f) {
                Altitude += _velocity.Y * (float) e.Time;
                _velocity.Y -= Sphere.GetGravitationalAcceleration(Altitude) * (float) e.Time;
            } else {
                Altitude = EyeHeight;
                _velocity.Y = 0f;
            }

            InvalidateViewMatrix();
        }
    }
}
