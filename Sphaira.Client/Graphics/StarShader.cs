using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    class StarShader : CelestialBodyShader<Star>
    {
        public StarShader()
        {
            DepthTest = false;
            Blend = true;
        }

        protected override void ConstructFragmentShader(ShaderBuilder frag)
        {
            frag.AddUniform(ShaderVarType.Vec4, "color");

            base.ConstructFragmentShader(frag);
        }

        protected override string GetRenderFuncSource()
        {
            return @"
                vec4 render(vec3 pos, vec3 cam)
                {
                    vec3 norm = normalize(pos);
                    vec3 dir = normalize(cam - pos);

                    vec3 lookdir = normalize(-cam);

                    float mag = dot(norm, dir);

                    vec3 up = cross(vec3(0, 1, 0), lookdir);
                    vec3 right = cross(up, lookdir);

                    float ang = atan(dot(dir, up), dot(dir, right));
                    float mul = pow(max(0, sin(ang * (2 +  2 * (int(body.w * 83) % 4)) + body.w * 91) * 0.6), 4) + pow(max(0, sin(ang * (7 + sin(body.w * 57) * 2) + body.w * 17) * 0.2), 2) + 0.1;
                    
                    return vec4(color.rgb, max(0, min(1, pow(mag, 512) + pow(mag, 16) * mul)));
                }
            ";
        }

        protected override void OnRender(Star body)
        {
            SetUniform("color", body.Color);

            base.OnRender(body);
        }
    }
}
