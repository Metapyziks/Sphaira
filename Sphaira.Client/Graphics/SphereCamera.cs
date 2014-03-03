using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTKTK.Scene;

namespace Sphaira.Client.Graphics
{
    public class SphereCamera : Camera
    {
        private Quaternion _sphereRot;

        public float Radius { get; private set; }

        public float Altitude { get; set; }

        public SphereCamera(int width, int height, float radius)
            : base(width, height)
        {
            Radius = radius;
            Altitude = 2f;

            _sphereRot = new Quaternion(Vector3.UnitY, 0f);
        }

        protected override void OnUpdatePerspectiveMatrix(ref OpenTK.Matrix4 matrix)
        {
            matrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver3,
                (float) Width / Height, 1f / 64f, 256f);
        }

        protected override void OnUpdateViewMatrix(ref Matrix4 matrix)
        {
            var yRot = Matrix4.CreateRotationY(Yaw);
            var xRot = Matrix4.CreateRotationX(Pitch);
            var trns = Matrix4.CreateTranslation(-Vector3.UnitY * (Radius + Altitude));
            var sRot = Matrix4.CreateFromQuaternion(_sphereRot);

            // Combine the matrices to find the view transformation
            matrix = Matrix4.Mult(sRot, Matrix4.Mult(trns, Matrix4.Mult(yRot, xRot)));
        }

        public void Move(Vector2 vec)
        {
            var vec3 = new Vector3(vec.X, 0f, vec.Y).Normalized();
            var norm = new Vector3(0f, 1f, 0f);

            var axis = Vector3.Cross(norm, -vec3);
            var arc = vec.Length / Radius;

            _sphereRot = Quaternion.Multiply(Quaternion.FromAxisAngle(axis, arc), _sphereRot);

            InvalidateViewMatrix();
        }
    }
}
