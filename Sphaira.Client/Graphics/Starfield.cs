using System;
using System.Drawing;
using System.IO;

using OpenTKTK.Textures;

namespace Sphaira.Client.Graphics
{
    public static class Starfield
    {
        public static CubeMapTexture Generate(int seed)
        {
            var format = String.Format("{0}{{0}}{{1}}.png", Path.Combine("res", "images", "sky"));

            var r = (Bitmap) Bitmap.FromFile(String.Format(format, "pos", "x"));
            var l = (Bitmap) Bitmap.FromFile(String.Format(format, "neg", "x"));
            var u = (Bitmap) Bitmap.FromFile(String.Format(format, "pos", "y"));
            var d = (Bitmap) Bitmap.FromFile(String.Format(format, "neg", "y"));
            var b = (Bitmap) Bitmap.FromFile(String.Format(format, "pos", "z"));
            var f = (Bitmap) Bitmap.FromFile(String.Format(format, "neg", "z"));

            return new CubeMapTexture(r, l, u, d, b, f);
        }
    }
}
