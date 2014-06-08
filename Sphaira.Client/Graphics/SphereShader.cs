using OpenTK;
using OpenTK.Graphics.OpenGL;

using OpenTKTK.Scene;
using OpenTKTK.Shaders;
using OpenTKTK.Utils;

using Sphaira.Shared.Geometry;

namespace Sphaira.Client.Graphics
{
    class SphereShader : CelestialBodyShader<Sphere>
    {
        private static SphereShader _sInstance;

        public static SphereShader Instance
        {
            get
            {
                if (_sInstance == null) {
                    _sInstance = new SphereShader();
                }

                return _sInstance;
            }
        }

        public SphereShader()
        {
            DepthTest = true;
            PrimitiveType = PrimitiveType.Quads;
        }

        protected override string GetRenderFuncSource()
        {
            return SkyShader.SunFragSource + @"
                vec4 render(vec3 pos, vec3 cam)
                {
                    vec3 normal = normalize(pos);
                    vec3 lookdir = normalize(pos - cam);

                    vec3 sundir = normalize(sun - body.xyz);
                    float light = max(1.0 / 32.0, dot(normal, sundir));
                    float checkSub = ((int(pos.x) + int(pos.y) + int(pos.z)) & 1);
                    float check = 1 - checkSub * 0.125;
                    vec3 clr = colour * (light_model.y * check * light * (1 - light_model.x) + light_model.x);

                    vec3 skypos = normalize(reflect(lookdir, normal));
                    vec3 sky = textureCubeLod(skybox, skypos, 2 - checkSub * 2).rgb;
                    sky = check * sky * light_model.w + (vec3(1, 1, 1) - sky * light_model.w) * getSun(skypos) * light_model.z;

                    return vec4(clr + (vec3(1, 1, 1) - clr) * sky, 1);
                }
            ";
        }

        protected override void ConstructFragmentShader(ShaderBuilder frag)
        {
            base.ConstructFragmentShader(frag);

            frag.AddUniform(ShaderVarType.Vec3, "sun");
            frag.AddUniform(ShaderVarType.Float, "time");
            frag.AddUniform(ShaderVarType.Vec4, "light_model");
            frag.AddUniform(ShaderVarType.Vec3, "colour");
            frag.AddUniform(ShaderVarType.SamplerCube, "skybox");
        }

        protected override void OnRender(Sphere body)
        {
            if (Camera is SphereCamera) {
                SetTexture("skybox", ((SphereCamera) Camera).SkyBox);
            }

            SetUniform("light_model", new Vector4(body.Ambient, body.Diffuse, body.Specular, body.Reflect));
            SetUniform("colour", body.Colour);

            base.OnRender(body);
        }
    }
}
