using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        public static CubeMapTexture Generate(int seed, int resolution = 1024)
        {
            var rand = new Random(seed);

            int count = 16384;
            var stars = new List<Sphere>(count);

            float near = 8f;
            float far = 32f;

            float minRad = 1f / 64f;
            float maxRad = 1f / 32f;

            for (int i = 0; i < count; ++i) {
                var dist = far - (far - near) * rand.NextSingle() * rand.NextSingle();

                Vector3 norm;

                do {
                    norm = 2f * new Vector3(
                        rand.NextSingle() - .5f,
                        rand.NextSingle() - .5f,
                        rand.NextSingle() - .5f);
                } while (norm.LengthSquared > 1f);

                norm.Normalize();

                var star = new Sphere(norm * dist, minRad + rand.NextSingle() * rand.NextSingle() * (maxRad - minRad), 0f);

                float shift = rand.NextSingle() * 2f - 1f;
                
                star.Ambient = 1f;
                star.Colour = new Vector3(0.9f + shift * 0.1f, 1f - Math.Abs(shift) * 0.3f, 0.9f - shift * 0.1f);
                star.Diffuse = 0f;
                star.Specular = 0f;
                star.Reflect = 0f;

                stars.Add(star);
            }
                
            var camera = new Camera(resolution, resolution, MathHelper.PiOver2, near - maxRad, far + maxRad);
            var sphereShader = new SphereShader { Camera = camera };
            var target = new FrameBuffer(new BitmapTexture2D(new Bitmap(resolution, resolution)), 16);

            var angles = new[] {
                Tuple.Create(0f, MathHelper.PiOver2),
                Tuple.Create(0f, -MathHelper.PiOver2),
                Tuple.Create(MathHelper.PiOver2, 0f),
                Tuple.Create(-MathHelper.PiOver2, 0f),
                Tuple.Create(0f, MathHelper.Pi),
                Tuple.Create(0f, 0f)
            };

            var bmps = new Bitmap[6];

            target.Begin();
            for (int i = 0; i < 6; ++i) {
                camera.Pitch = angles[i].Item1;
                camera.Yaw = angles[i].Item2;

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                foreach (var star in stars) {
                    sphereShader.Render(star);
                }

                var bmp = bmps[i] = new Bitmap(resolution, resolution);

                var data = bmp.LockBits(new Rectangle(0, 0, resolution, resolution), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.ReadPixels(0, 0, resolution, resolution, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bmp.UnlockBits(data);
            }
            target.End();

            target.Dispose();
            sphereShader.Dispose();

            return new CubeMapTexture(bmps[0], bmps[1], bmps[2], bmps[3], bmps[4], bmps[5]);
        }
    }
}
