using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTKTK.Scene;
using OpenTKTK.Shaders;
using OpenTKTK.Textures;
using OpenTKTK.Utils;
using Lidgren.Network;
using Sphaira.Client.Graphics;
using Sphaira.Shared.Geometry;
using System.Threading;
using Sphaira.Client.Network;

namespace Sphaira.Client
{
    public class Program : GameWindow
    {
        private class DebugListener : TraceListener
        {
            public override void Write(string message)
            {
                Console.Write(message);
            }

            public override void WriteLine(string message)
            {
                Console.WriteLine(message);
            }
        }

        const float StandEyeLevel = 1.7f;
        const float CrouchEyeLevel = 0.8f;

        private static int _cSkySeed;

        public static int Main(String[] args)
        {
            Trace.Listeners.Add(new DebugListener());

            NetWrapper.RegisterMessageHandler("WorldInfo", msg => {
                _cSkySeed = msg.ReadInt32();
            });

            NetWrapper.Connect("localhost", 14242);
            NetWrapper.SendMessage("WorldInfo", NetDeliveryMethod.ReliableUnordered);

            _cSkySeed = 0;

            while (_cSkySeed == 0) {
                if (!NetWrapper.CheckForMessages()) Thread.Sleep(16);
            }

            using (var app = new Program()) {
                app.Run();
            }

            return 0;
        }

        private SphereCamera _camera;
        private Sphere _sphere;

        private FrameBuffer[] _frameBuffers;

        private Stopwatch _frameTimer;
        private Stopwatch _timer;
        private int _frameCounter;

        private bool _captureMouse;
        private bool _takeScreenShot;

        public Program() : base(1280, 720, new GraphicsMode(new ColorFormat(8, 8, 8, 0), 32))
        {
            Title = "Sphaira";
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(ClientRectangle);

            _camera.SetScreenSize(Width, Height);

            if (_frameBuffers != null) {
                foreach (var buffer in _frameBuffers) {
                    buffer.Dispose();
                }
            }

            _frameBuffers = new FrameBuffer[2];
            for (int i = 0; i < 2; ++i) {
                _frameBuffers[i] = new FrameBuffer(new BitmapTexture2D(Width, Height) {
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    TextureWrapS = TextureWrapMode.ClampToEdge,
                    TextureWrapT = TextureWrapMode.ClampToEdge
                });
            }

            BloomShader.Instance.SetScreenSize(Width, Height);
            FxAAShader.Instance.SetScreenSize(Width, Height);
        }

        protected override void OnLoad(EventArgs e)
        {
            _sphere = new Sphere(Vector3.Zero, 8f, 1024f);

            _camera = new SphereCamera(Width, Height, _sphere, StandEyeLevel);
            _camera.SkyBox = Starfield.Generate(_cSkySeed);

            _frameTimer = new Stopwatch();
            _timer = new Stopwatch();

            _frameTimer.Start();
            _timer.Start();

            _frameCounter = 0;

            Mouse.Move += (sender, me) => {
                var centre = new Point(Bounds.Left + Width / 2, Bounds.Top + Height / 2);

                if (!Focused || !_captureMouse) return;
                if (Cursor.Position.X == centre.X && Cursor.Position.Y == centre.Y) return;

                _camera.Yaw += me.XDelta / 360f;
                _camera.Pitch += me.YDelta / 360f;

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
                    case Key.F11:
                        if (WindowState == WindowState.Fullscreen) {
                            WindowState = WindowState.Normal;
                        } else {
                            WindowState = WindowState.Fullscreen;
                        }
                        break;
                    case Key.F12:
                        _takeScreenShot = true;
                        break;
                }
            };
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            NetWrapper.CheckForMessages();

            var move = Vector3.Zero;

            if (Keyboard[Key.W]) move -= Vector3.UnitZ;
            if (Keyboard[Key.S]) move += Vector3.UnitZ;
            if (Keyboard[Key.A]) move -= Vector3.UnitX;
            if (Keyboard[Key.D]) move += Vector3.UnitX;

            if (Keyboard[Key.ControlLeft]) {
                _camera.EyeHeight = CrouchEyeLevel;
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

        private void TakeScreenShot()
        {
            var bmp = new Bitmap(Width, Height);
            var data = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.ReadPixels(0, 0, Width, Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            var dateName = DateTime.Now.ToString()
                .Replace("/", "-")
                .Replace(" ", "-")
                .Replace(":", "-");

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bmp.Save(String.Format("sphaira-{0}-{1:D3}.png", dateName, DateTime.Now.Millisecond), ImageFormat.Png);

            bmp.Dispose();

            _takeScreenShot = false;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            var sun = Vector3.Transform(Vector3.UnitX * 8192f,
                Quaternion.FromAxisAngle(Vector3.UnitY, (float) _timer.Elapsed.TotalSeconds / 12f));

            _frameBuffers[0].Begin();
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                var skyShader = SkyShader.Instance;
                skyShader.Camera = _camera;
                skyShader.SetUniform("time", (float) _timer.Elapsed.TotalSeconds);
                skyShader.SetUniform("sun", sun);
                skyShader.Render();

                var sphereShader = SphereShader.Instance;
                sphereShader.Camera = _camera;
                sphereShader.BeginBatch();
                    sphereShader.SetUniform("time", (float) _timer.Elapsed.TotalSeconds);
                    sphereShader.SetUniform("sun", sun);
                    sphereShader.Render(_sphere);
                sphereShader.EndBatch();
            _frameBuffers[0].End();

            _frameBuffers[1].Begin();
                BloomShader.Instance.FrameTexture = (BitmapTexture2D) _frameBuffers[0].Texture;
                BloomShader.Instance.Render();
            _frameBuffers[1].End();

            FxAAShader.Instance.FrameTexture = (BitmapTexture2D) _frameBuffers[1].Texture;
            FxAAShader.Instance.Render();

            if (_takeScreenShot) TakeScreenShot();

            SwapBuffers();
            ++_frameCounter;
        }
    }
}
