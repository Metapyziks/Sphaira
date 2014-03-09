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
using System.Collections.Generic;
using System.Linq;

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

        private class PlayerInfo
        {
            public ushort ID { get; private set; }

            public Sphere Sphere { get; private set; }

            public Vector3 Position { get; set; }

            public PlayerInfo(ushort id)
            {
                ID = id;
                Sphere = new Sphere(Vector3.Zero, 0.5f, 0f);
                Position = Vector3.Zero;
            }

            public void UpdateFrame(FrameEventArgs e)
            {
                Sphere.Position += (Position - Sphere.Position) * 0.25f;
            }
        }

        const float StandEyeLevel = 1.7f;
        const float CrouchEyeLevel = 0.8f;

        private static Stopwatch _sTimer;
        private static double _sTimerOffset;

        public static double Time
        {
            get { return _sTimer.Elapsed.TotalSeconds + _sTimerOffset; }
        }

        private static int _sSkySeed;
        private static ushort _sMyID;
        private static int _sQuality;

        private static Dictionary<ushort, PlayerInfo> _sPlayers;

        public static int Main(String[] args)
        {
            Trace.Listeners.Add(new DebugListener());

            _sTimer = new Stopwatch();

            NetWrapper.RegisterMessageHandler("WorldInfo", msg => {
                _sSkySeed = msg.ReadInt32();
                _sTimerOffset = msg.ReadDouble() + NetWrapper.AverageRoundTripTime * 0.5;
                _sMyID = msg.ReadUInt16();

                _sTimer.Restart();
            });

            NetWrapper.RegisterMessageHandler("PlayerInfo", msg => {
                var count = msg.ReadUInt16();
                var players = new ushort[count];

                for (int i = 0; i < count; ++i) {
                    var id = players[i] = msg.ReadUInt16();

                    if (id != _sMyID && !_sPlayers.ContainsKey(id)) {
                        _sPlayers.Add(id, new PlayerInfo(id));
                    }
                }

                var removed = _sPlayers.Keys.Where(x => x == _sMyID || !players.Contains(x)).ToArray();

                foreach (var id in removed) {
                    _sPlayers.Remove(id);
                }
            });

            NetWrapper.RegisterMessageHandler("PlayerPos", msg => {
                var id = msg.ReadUInt16();

                if (_sPlayers.ContainsKey(id)) {
                    var player = _sPlayers[id];
                    player.Position = new Vector3(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
                }
            });

            _sSkySeed = 0;
            _sMyID = 0xffff;
            _sPlayers = new Dictionary<ushort, PlayerInfo>();

            var qualities = new[] {
                "Basic",
                "FxAA",
                "FxAA + Bloom"
            };

            Console.WriteLine("Select a display quality:");
            for (int i = 0; i < qualities.Length; ++i) {
                Console.WriteLine("{0}. {1}", i + 1, qualities[i]);
            }

            while (!int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out _sQuality) ||
                _sQuality < 1 || _sQuality > qualities.Length);

            _sQuality -= 1;

            Console.WriteLine("Selected {0}", qualities[_sQuality]);
            Console.WriteLine();

            Console.Write("Server hostname: ");
            NetWrapper.Connect(Console.ReadLine(), 14242);
            NetWrapper.SendMessage("WorldInfo", NetDeliveryMethod.ReliableOrdered, 0);
            
            while (_sSkySeed == 0) {
                if (!NetWrapper.CheckForMessages()) Thread.Sleep(16);
            }

            using (var app = new Program()) {
                app.Run();
            }

            NetWrapper.Disconnect();

            return 0;
        }

        private SphereCamera _camera;
        private Sphere _sphere;

        private FrameBuffer[] _frameBuffers;

        private Stopwatch _frameTimer;
        private Stopwatch _lastPosUpdateTimer;

        private int _frameCounter;
        private Vector3 _oldPos;

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

            _frameBuffers = new FrameBuffer[_sQuality];
            for (int i = 0; i < _sQuality; ++i) {
                _frameBuffers[i] = new FrameBuffer(new BitmapTexture2D(Width, Height) {
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    TextureWrapS = TextureWrapMode.ClampToEdge,
                    TextureWrapT = TextureWrapMode.ClampToEdge
                }, i == 0 ? 16 : 0);
            }

            if (_sQuality > 0) {
                FxAAShader.Instance.SetScreenSize(Width, Height);
            }

            if (_sQuality > 1) {
                BloomShader.Instance.SetScreenSize(Width, Height);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            _sphere = new Sphere(Vector3.Zero, 8f, 1024f);

            _camera = new SphereCamera(Width, Height, _sphere, StandEyeLevel);
            _camera.SkyBox = Starfield.Generate(_sSkySeed);

            _frameTimer = new Stopwatch();
            _lastPosUpdateTimer = new Stopwatch();

            _frameTimer.Start();
            _lastPosUpdateTimer.Start();

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
                    move *= 16f;
                } else if (Keyboard[Key.ShiftLeft]) {
                    move *= 48f;
                } else {
                    move *= 24f;
                }


                if (_camera.Altitude > _camera.EyeHeight) {
                    move *= 1f / 8f;
                }

                _camera.Push(move.Xz * (float) e.Time);
            }

            _camera.UpdateFrame(e);

            foreach (var player in _sPlayers.Values) {
                player.UpdateFrame(e);
            }

            var pos = _camera.Position;
            var period = _lastPosUpdateTimer.Elapsed.TotalSeconds;
            if (period > 1.0 || (period > 1.0 / 30.0 && (_oldPos - pos).Length > 1f / 16f)) {
                NetWrapper.SendMessage("PlayerPos", msg => {
                    msg.Write(pos.X);
                    msg.Write(pos.Y);
                    msg.Write(pos.Z);
                }, NetDeliveryMethod.ReliableSequenced, 1);

                _oldPos = pos;
                _lastPosUpdateTimer.Restart();
            }
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
                Quaternion.FromAxisAngle(Vector3.UnitY, (float) Time / 12f));

            if (_sQuality > 0) _frameBuffers[0].Begin();

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                var skyShader = SkyShader.Instance;
                skyShader.Camera = _camera;
                skyShader.SetUniform("time", (float) Time);
                skyShader.SetUniform("sun", sun);
                skyShader.Render();

                var sphereShader = SphereShader.Instance;
                sphereShader.DepthTest = true;
                sphereShader.Camera = _camera;

                sphereShader.BeginBatch();

                    sphereShader.SetUniform("time", (float) Time);
                    sphereShader.SetUniform("sun", sun);
                    sphereShader.Render(_sphere);

                    foreach (var player in _sPlayers.Values) {
                        sphereShader.Render(player.Sphere);
                    }

                sphereShader.EndBatch();

            if (_sQuality > 0) _frameBuffers[0].End();

            if (_sQuality > 1) {
                _frameBuffers[1].Begin();
                BloomShader.Instance.FrameTexture = (BitmapTexture2D) _frameBuffers[0].Texture;
                BloomShader.Instance.Render();
                _frameBuffers[1].End();
            }

            if (_sQuality > 0) {
                FxAAShader.Instance.FrameTexture = (BitmapTexture2D) _frameBuffers.Last().Texture;
                FxAAShader.Instance.Render();
            }

            if (_takeScreenShot) TakeScreenShot();

            SwapBuffers();
            ++_frameCounter;
        }
    }
}
