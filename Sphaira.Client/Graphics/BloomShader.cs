using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphaira.Client.Graphics
{
    public class BloomShader : PostProcessShader
    {
        private static BloomShader _sInstance;

        public static BloomShader Instance
        {
            get
            {
                if (_sInstance == null) {
                    _sInstance = new BloomShader();
                }

                return _sInstance;
            }
        }

        protected override void ConstructFragmentShader(OpenTKTK.Utils.ShaderBuilder frag)
        {
            base.ConstructFragmentShader(frag);

            frag.Logic = @"
                void main(void)
                {
                    const int samples = 16;
                    const float quality = 2;

                    vec4 source = texture2D(frame, var_texcoord);
                    vec4 sum = vec4(0);
                    int diff = (samples - 1) / 2;
                    vec2 sizeFactor = vec2(1) / screen_resolution * quality;
  
                    float tot = 0;
                    float rcpQual2 = 1 / (quality * quality);

                    for (int x = -diff; x <= diff; ++x) {
                        for (int y = -diff; y <= diff; ++y) {
                            if (x * x + y * y > diff * diff) continue;
                            vec2 offset = vec2(x, y) * sizeFactor;
                            float mul = 1 / max(1, sqrt(x * x * rcpQual2 + y * y * rcpQual2));
                            sum += texture2D(frame, var_texcoord + offset) * mul;
                            tot += mul;
                        }
                    }
  
                    out_colour = sum / tot + source;
                }
            ";
        }
    }
}
