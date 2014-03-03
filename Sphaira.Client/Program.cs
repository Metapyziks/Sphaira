using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private TestShader _testShader;
        private EllipseShader _ellipseShader;
        private Sphere _sphere;
        private Stopwatch _timer;

        private bool _captureMouse;

        public Program() : base(1280, 720)
        {
            Title = "Sphaira";
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(ClientRectangle);

            _testShader.Camera.SetScreenSize(Width, Height);
        }

        protected override void OnLoad(EventArgs e)
        {
            _sphere = new Sphere(32f, 1024f);

            var camera = new SphereCamera(Width, Height, _sphere, 4f);

            _testShader = new TestShader();
            _testShader.Camera = camera;

            _ellipseShader = new EllipseShader();
            _ellipseShader.Camera = camera;

            _timer = new Stopwatch();
            _timer.Start();

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
                    case Key.Space:
                        if (_testShader.Camera.Altitude <= _testShader.Camera.EyeHeight) {
                            _testShader.Camera.Jump(32f);
                        }
                        break;
                }
            };
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            var move = Vector3.Zero;
            var camera = _testShader.Camera;

            if (Keyboard[Key.W]) move -= Vector3.UnitZ;
            if (Keyboard[Key.S]) move += Vector3.UnitZ;
            if (Keyboard[Key.A]) move -= Vector3.UnitX;
            if (Keyboard[Key.D]) move += Vector3.UnitX;

            if (move.LengthSquared > 0f) {
                var rot = Matrix4.CreateRotationY(-camera.Yaw);

                move = Vector3.Transform(move.Normalized(), rot);

                if (Keyboard[Key.ShiftLeft]) {
                    move *= 4f;
                } else {
                    move *= 2f;
                }

                if (camera.Altitude > camera.EyeHeight) {
                    move *= 1f / 8f;
                }

                camera.Push(move.Xz);
            }

            camera.UpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var ang = Quaternion.FromAxisAngle(new Vector3(0f, 1f, 0f),
                (float) (_timer.Elapsed.TotalSeconds * Math.PI / 10.0));

            _testShader.SetUniform("sun", Vector3.Transform(new Vector3(_sphere.Radius + 512f, 0f, 0f), ang));

            _sphere.Render(_testShader);
            _ellipseShader.Render(_sphere);

            SwapBuffers();
        }
    }
}
