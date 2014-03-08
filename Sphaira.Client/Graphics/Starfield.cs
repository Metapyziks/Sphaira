using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Scene;
using OpenTKTK.Textures;
using OpenTKTK.Utils;
using Sphaira.Shared.Geometry;

namespace Sphaira.Client.Graphics
{
    public static class Starfield
    {
        private static Vector3 GetRandomPosition(this Random rand, float near, float far)
        {
            var dist = far - (far - near) * rand.NextSingle() * rand.NextSingle();

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

        private static Sphere[] GenerateStars(this Random rand, int count, float near, float far)
        {
            float minRad = 1f / 64f;
            float maxRad = 1f / 32f;

            var stars = new Sphere[count];

            for (int i = 0; i < count; ++i) {
                var pos = rand.GetRandomPosition(near, far);
                var star = new Sphere(pos, minRad + rand.NextSingle() * rand.NextSingle() * (maxRad - minRad), 0f);

                float shift = rand.NextSingle() * 2f - 1f;

                star.Ambient = 1f;
                star.Colour = new Vector3(0.9f + shift * 0.1f, 1f - Math.Abs(shift) * 0.3f, 0.9f - shift * 0.1f);
                star.Diffuse = 0f;
                star.Specular = 0f;
                star.Reflect = 0f;

                stars[i] = star;
            }

            return stars;
        }

        private static Nebula[] GenerateNebulae(this Random rand, int count, float near, float far)
        {
            var clrPairs = new Color4[,] {
                { Color4.Red, Color4.Blue },
                { Color4.Blue, Color4.Yellow },
                { Color4.Yellow, Color4.Red }
            };

            float minRad = 1f;
            float maxRad = 16f;

            int variation = rand.Next(1, 32);

            var seta = new Vector3[variation];
            var setb = new Vector3[variation];
            for (int i = 0; i < variation; ++i) {
                seta[i] = rand.GetRandomPosition(near, far);
                setb[i] = rand.GetRandomPosition(near, far);
            }

            int index = rand.Next(clrPairs.GetLength(0));
            var clra = clrPairs[index, 0];
            var clrb = clrPairs[index, 1];

            var nebulae = new Nebula[count];
            for (int i = 0; i < count; ++i) {
                var pos = rand.GetRandomPosition(near, far);
                var suma = seta.Sum(x => 1f / Math.Max(0.125f, (pos - x).LengthSquared));
                var sumb = setb.Sum(x => 1f / Math.Max(0.125f, (pos - x).LengthSquared));

                var shift = (suma * suma) / ((suma * suma) + (sumb * sumb));
                var rad = rand.NextSingle(minRad, maxRad);

                nebulae[i] = new Nebula(pos, rad,
                    new Color4(
                        clra.R * shift + clrb.R * (1f - shift),
                        clra.G * shift + clrb.G * (1f - shift),
                        clra.B * shift + clrb.B * (1f - shift),
                        1f / 128f + rand.NextSingle() * rand.NextSingle() * (1f / 16f) * (rad / maxRad)));
            }

            return nebulae;
        }

        public static CubeMapTexture Generate(int seed, int resolution = 1024)
        {
            var rand = new Random(seed);

            float near = 8f;
            float far = 32f;

            var stars = rand.GenerateStars(rand.Next(512, 32768), near, far);
            var nebulae = rand.GenerateNebulae(8192, near, far);
            
            var camera = new Camera(resolution, resolution, MathHelper.PiOver2, 4f, 64f);
            SphereShader.Instance.Camera = camera;
            NebulaShader.Instance.Camera = camera;
            var target = new FrameBuffer(new BitmapTexture2D(new Bitmap(resolution, resolution)), 16);

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
            for (int i = 0; i < 6; ++i) {
                camera.Pitch = angles[i].Item1;
                camera.Yaw = angles[i].Item2;

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                NebulaShader.Instance.BeginBatch();
                foreach (var nebula in nebulae) {
                    NebulaShader.Instance.Render(nebula);
                }
                NebulaShader.Instance.EndBatch();

                SphereShader.Instance.BeginBatch();
                foreach (var star in stars) {
                    SphereShader.Instance.Render(star);
                }
                SphereShader.Instance.EndBatch();

                var bmp = bmps[i] = new Bitmap(resolution, resolution);

                var data = bmp.LockBits(new Rectangle(0, 0, resolution, resolution), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.ReadPixels(0, 0, resolution, resolution, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bmp.UnlockBits(data);
            }
            target.End();

            target.Dispose();

            return new CubeMapTexture(bmps[0], bmps[1], bmps[2], bmps[3], bmps[4], bmps[5]);
        }
    }
}
