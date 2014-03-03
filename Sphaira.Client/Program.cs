using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTKTK.Utils;
using Sphaira.Client.Geometry;
using Sphaira.Client.Graphics;

namespace Sphaira.Client
{
    public class Program : GameWindow
    {
        public static int Main(String[] args)
        {
            using (var app = new Program()) {
                app.Run();
            }

            return 0;
        }

        private TestShader _shader;
        private Sphere _sphere;

        private bool _captureMouse;

        public Program() : base(1280, 720)
        {
            Title = "Sphaira";
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(ClientRectangle);

            _shader.Camera.SetScreenSize(Width, Height);
        }

        protected override void OnLoad(EventArgs e)
        {
            _sphere = new Sphere(256f, 1f);

            var camera = new SphereCamera(Width, Height, _sphere.Radius);
            camera.Altitude = 4f;

            _shader = new TestShader();
            _shader.Camera = camera;


            Mouse.Move += (sender, me) => {
                var centre = new Point(Bounds.Left + Width / 2, Bounds.Top + Height / 2);

                if (!Focused || !_captureMouse) return;
                if (Cursor.Position.X == centre.X && Cursor.Position.Y == centre.Y) return;

                camera.Yaw += (Cursor.Position.X - centre.X) / 360f;
                camera.Pitch += (Cursor.Position.Y - centre.Y) / 360f;

                camera.Pitch = Tools.Clamp(camera.Pitch, -MathHelper.PiOver2, MathHelper.PiOver2);

                Cursor.Position = centre;
            };

            Mouse.ButtonUp += (sender, me) => {
                if (_captureMouse) return;

                _captureMouse = true;
                Cursor.Hide();
            };

            Keyboard.KeyDown += (sender, ke) => {
                switch (ke.Key) {
                    case Key.Escape:
                        _captureMouse = !_captureMouse;
                        if (_captureMouse) Cursor.Hide(); else Cursor.Show();
                        break;
                }
            };
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            var move = Vector3.Zero;
            var camera = _shader.Camera;

            if (Keyboard[Key.W]) move -= Vector3.UnitZ;
            if (Keyboard[Key.S]) move += Vector3.UnitZ;
            if (Keyboard[Key.A]) move -= Vector3.UnitX;
            if (Keyboard[Key.D]) move += Vector3.UnitX;

            if (move.LengthSquared > 0f) {
                var rot = Matrix4.CreateRotationY(-camera.Yaw);

                move = Vector3.Transform(move.Normalized(), rot);

                if (Keyboard[Key.ShiftLeft]) {
                    move *= (float) (32f * e.Time);
                } else {
                    move *= (float) (8f * e.Time);
                }

                camera.Move(move.Xz);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _sphere.Render(_shader);

            SwapBuffers();
        }
    }
}
