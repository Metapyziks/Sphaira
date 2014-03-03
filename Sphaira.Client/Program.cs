using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTKTK.Utils;
using Sphaira.Client.Graphics;
using Sphaira.Shared.Geometry;

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

        private SphereCamera _camera;
        private SphereShader _sphereShader;
        private Sphere _sphere;
        private Stopwatch _timer;

        private bool _captureMouse;

        public Program() : base(1280, 720, new GraphicsMode(new ColorFormat(8, 8, 8, 0), 32))
        {
            Title = "Sphaira";
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(ClientRectangle);

            _camera.SetScreenSize(Width, Height);
        }

        protected override void OnLoad(EventArgs e)
        {
            _sphere = new Sphere(8f, 1024f);

            _camera = new SphereCamera(Width, Height, _sphere, 1.6625f);

            _sphereShader = new SphereShader();
            _sphereShader.Camera = _camera;

            _timer = new Stopwatch();
            _timer.Start();

            Mouse.Move += (sender, me) => {
                var centre = new Point(Bounds.Left + Width / 2, Bounds.Top + Height / 2);

                if (!Focused || !_captureMouse) return;
                if (Cursor.Position.X == centre.X && Cursor.Position.Y == centre.Y) return;

                _camera.Yaw += (Cursor.Position.X - centre.X) / 360f;
                _camera.Pitch += (Cursor.Position.Y - centre.Y) / 360f;

                _camera.Pitch = Tools.Clamp(_camera.Pitch, -MathHelper.PiOver2, MathHelper.PiOver2);

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
                        if (_camera.Altitude <= _camera.EyeHeight) {
                            _camera.Jump(8f);
                        }
                        break;
                    case Key.Z:
                        _camera = new SphereCamera(Width, Height, _sphere, _sphere.Radius);
                        _sphereShader.Camera = _camera;
                        break;
                    case Key.F11:
                        if (WindowState == WindowState.Fullscreen) {
                            WindowState = WindowState.Normal;
                        } else {
                            WindowState = WindowState.Fullscreen;
                        }
                        break;
                }
            };
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            var move = Vector3.Zero;

            if (Keyboard[Key.W]) move -= Vector3.UnitZ;
            if (Keyboard[Key.S]) move += Vector3.UnitZ;
            if (Keyboard[Key.A]) move -= Vector3.UnitX;
            if (Keyboard[Key.D]) move += Vector3.UnitX;

            if (move.LengthSquared > 0f) {
                var rot = Matrix4.CreateRotationY(-_camera.Yaw);

                move = Vector3.Transform(move.Normalized(), rot);

                if (Keyboard[Key.ShiftLeft]) {
                    move *= 4f;
                } else {
                    move *= 2f;
                }

                if (_camera.Altitude > _camera.EyeHeight) {
                    move *= 1f / 8f;
                }

                _camera.Push(move.Xz);
            }

            _camera.UpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _sphere.Radius = 16f + (float) Math.Sin(_timer.Elapsed.TotalSeconds / 4.0) * 8f;
            
            _sphereShader.SetUniform("sun", -Vector3.UnitY);
            _sphereShader.Render(_sphere);

            SwapBuffers();
        }
    }
}
