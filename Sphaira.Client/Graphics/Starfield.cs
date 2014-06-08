using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using OpenTKTK.Scene;
using OpenTKTK.Textures;
using OpenTKTK.Utils;

using Sphaira.Shared.Geometry;

namespace Sphaira.Client.Graphics
{
    public static class Starfield
    {
        public static readonly Comparer<CelestialBody> Comparer =
            Comparer<CelestialBody>.Create((a, b) => {
                var av = a.Position.LengthSquared;
                var bv = b.Position.LengthSquared;

                return av < bv ? -1 : av == bv ? 0 : 1;
            });

        public static Vector3 GetRandomPosition(Random rand, float near, float far)
        {
            var dist = far - (far - near) * (float) Math.Pow(rand.NextSingle(), 3);

            Vector3 norm;

            do {
                norm = 2f * new Vector3(
                    rand.NextSingle() - .5f,
                    rand.NextSingle() - .5f,
                    rand.NextSingle() - .5f);
            } while (norm.LengthSquared > 1f);

            norm.Normalize();

            return norm.Normalized() * dist;
        }

        public static CubeMapTexture Generate(int seed, int resolution, int samples)
        {
            var rand = seed == 0 ? new Random() : new Random(seed);

            var stars = Star.Generate(rand, rand.Next(16384, 32768));
            var dusts = DustCloud.Generate(rand, rand.Next(4096, 8192));

            int renderResolution = resolution * samples;

            var camera = new Camera(renderResolution, renderResolution, MathHelper.PiOver2, 4f, 64f);

            var starShader = new StarShader { Camera = camera };
            var dustShader = new DustCloudShader { Camera = camera };

            var target = new FrameBuffer(new BitmapTexture2D(new Bitmap(renderResolution, renderResolution)), 16);

            var angles = new[] {
                Tuple.Create(0f, MathHelper.PiOver2),
                Tuple.Create(0f, -MathHelper.PiOver2),
                Tuple.Create(MathHelper.PiOver2, 0f),
                Tuple.Create(-MathHelper.PiOver2, 0f),
                Tuple.Create(0f, 0f),
                Tuple.Create(0f, MathHelper.Pi)
            };

            var bmps = new Bitmap[6];

            target.Begin();

            GL.ClearColor(Color.Black);

            var bmp = new Bitmap(renderResolution, renderResolution);

            for (int i = 0; i < 6; ++i) {
                camera.Pitch = angles[i].Item1;
                camera.Yaw = angles[i].Item2;

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                dustShader.BeginBatch();
                foreach (var dust in dusts) {
                    dustShader.Render(dust);
                }
                dustShader.EndBatch();

                starShader.BeginBatch();
                foreach (var star in stars) {
                    starShader.Render(star);
                }
                starShader.EndBatch();

                var data = bmp.LockBits(new Rectangle(0, 0, renderResolution, renderResolution), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.ReadPixels(0, 0, renderResolution, renderResolution, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bmp.UnlockBits(data);

                bmps[i] = new Bitmap(bmp, new Size(resolution, resolution));
            }

            bmp.Dispose();
            target.End();
            target.Dispose();

            return new CubeMapTexture(bmps[0], bmps[1], bmps[2], bmps[3], bmps[4], bmps[5]);
        }
    }
}
