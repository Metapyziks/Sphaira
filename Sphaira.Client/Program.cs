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
        const float StandEyeLevel = 1.7f;
        const float CrouchEyeLevel = 0.8f;

        public static int Main(String[] args)
        {
            using (var app = new Program()) {
                app.Run();
            }

            return 0;
        }

        private SphereCamera _camera;
        private SkyShader _skyShader;
        private SphereShader _sphereShader;
        private Sphere _sphere;
        private Sphere _satellite;
        private Stopwatch _frameTimer;
        private Stopwatch _timer;
        private int _frameCounter;

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
            _sphere = new Sphere(Vector3.Zero, 8f, 1024f);
            _satellite = new Sphere(Vector3.Zero, 32f, 1024f);

            _camera = new SphereCamera(Width, Height, _sphere, StandEyeLevel);
            _camera.SkyBox = Starfield.Generate(0x4af618a);

            _sphereShader = new SphereShader();
            _sphereShader.Camera = _camera;

            _skyShader = new SkyShader();
            _skyShader.Camera = _camera;

            _frameTimer = new Stopwatch();
            _timer = new Stopwatch();

            _frameTimer.Start();
            _timer.Start();

            _frameCounter = 0;

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
                    case Key.Number0:
                    case Key.Number1:
                    case Key.Number2:
                    case Key.Number3:
                    case Key.Number4:
                    case Key.Number5:
                    case Key.Number6:
                    case Key.Number7:
                    case Key.Number8:
                    case Key.Number9:
                        var n = (int) ke.Key - (int) Key.Number0;
                        _sphere.Radius = (float) Math.Pow(2, n + 1);
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

            if (Keyboard[Key.ControlLeft]) {
                _camera.EyeHeight = _sphere.Radius * 1.5f;
            } else {
                _camera.EyeHeight += (StandEyeLevel - _camera.EyeHeight) * 0.25f;
            }

            if (_frameTimer.Elapsed.TotalSeconds > 0.5) {
                _frameTimer.Stop();

                Title = String.Format("FPS: {0:F2}", _frameCounter / _frameTimer.Elapsed.TotalSeconds);
                
                _frameTimer.Restart();
                _frameCounter = 0;
            }

            if (move.LengthSquared > 0f) {
                var rot = Matrix4.CreateRotationY(-_camera.Yaw);

                move = Vector3.Transform(move.Normalized(), rot);

                if (Keyboard[Key.ControlLeft]) {
                    move *= 64f;
                } else if (Keyboard[Key.ShiftLeft]) {
                    move *= 16f;
                } else {
                    move *= 48f;
                }


                if (_camera.Altitude > _camera.EyeHeight) {
                    move *= 1f / 8f;
                }

                _camera.Push(move.Xz * (float) e.Time);
            }

            _camera.UpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _skyShader.Render();

            _sphereShader.SetUniform("sun", -Vector3.UnitY);

            float dist = (_satellite.Radius + _sphere.Radius);
            _satellite.Position = Vector3.Transform(Vector3.UnitX * dist, Quaternion.FromAxisAngle(Vector3.UnitY, (float) _timer.Elapsed.TotalMinutes));

            _sphereShader.Render(_sphere);
            _sphereShader.Render(_satellite);

            SwapBuffers();
            ++_frameCounter;
        }
    }
}
