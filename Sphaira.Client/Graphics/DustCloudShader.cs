using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    class DustCloudShader : CelestialBodyShader<DustCloud>
    {
        public DustCloudShader()
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

                    return vec4(color.rgb, pow(dot(norm, dir), 2) * color.a);
                }
            ";
        }

        protected override void OnRender(DustCloud body)
        {
            SetUniform("color", body.Color);

            base.OnRender(body);
        }
    }
}
