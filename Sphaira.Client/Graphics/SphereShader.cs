using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Scene;
using OpenTKTK.Shaders;
using OpenTKTK.Textures;
using OpenTKTK.Utils;
using Sphaira.Shared.Geometry;

namespace Sphaira.Client.Graphics
{
    public class SphereShader : ShaderProgram3D<Camera>
    {
        private static VertexBuffer _sVB;
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

        public bool DepthTest { get; set; }

        public SphereShader()
        {
            DepthTest = true;
            BeginMode = BeginMode.Quads;
        }

        protected override void ConstructVertexShader(ShaderBuilder vert)
        {
            base.ConstructVertexShader(vert);

            vert.AddUniform(ShaderVarType.Vec4, "sphere");
            vert.AddAttribute(ShaderVarType.Vec2, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec3, "var_position");
            vert.Logic = @"
                void main(void)
                {
                    float r = sphere.w;
                    vec3 cam = camera - sphere.xyz;
                    float dist = length(cam);
                    float hyp = sqrt(dist * dist - r * r);
                    float ang = atan(r / hyp);
                    float opp = hyp * sin(ang);
                    float dif = sqrt(r * r - opp * opp);

                    vec3 center = normalize(cam) * dif;
                    vec3 up = normalize(cross(cam, (view * vec4(0, 0, 1, 0)).xyz)) * opp;
                    vec3 right = normalize(cross(cam, up)) * opp;

                    var_position = vec3(center + in_vertex.x * right + in_vertex.y * up) / r;

                    gl_Position = proj * view * vec4(sphere.xyz + center + in_vertex.x * right + in_vertex.y * up, 1);
                }
            ";
        }

        protected override void ConstructFragmentShader(ShaderBuilder frag)
        {
            base.ConstructFragmentShader(frag);

            frag.AddUniform(ShaderVarType.Vec4, "sphere");
            frag.AddUniform(ShaderVarType.Vec3, "sun");
            frag.AddUniform(ShaderVarType.Float, "time");
            frag.AddUniform(ShaderVarType.Vec4, "light_model");
            frag.AddUniform(ShaderVarType.Vec3, "colour");
            frag.AddUniform(ShaderVarType.SamplerCube, "skybox");
            frag.Logic = SkyShader.GetSunSource + @"
                void main(void)
                {
                    float r = sphere.w;
                    vec3 cam = camera - sphere.xyz;
                    float len2 = dot(var_position, var_position);

                    if (len2 > 1) discard;

                    vec3 l = normalize(cam / r - var_position);

                    float b = dot(l, var_position);
                    float d = -b + sqrt(b * b - len2 + 1);

                    vec3 pos = (var_position + l * d) * r;
                    vec4 fin = proj * view * vec4(pos + sphere.xyz, 1);

                    gl_FragDepth = (fin.z / fin.w + 1) / 2;

                    vec3 normal = normalize(pos);
                    vec3 lookdir = normalize(pos - cam);
                    vec3 sundir = normalize(sun - camera);
                    float light = max(1.0 / 32.0, dot(normal, sundir));
                    float check = 1 - ((int(pos.x) + int(pos.y) + int(pos.z)) & 1) * 0.125;
                    vec3 clr = colour * (light_model.y * check * light * (1 - light_model.x) + light_model.x);

                    vec3 skypos = normalize(reflect(lookdir, normal));
                    vec3 sky = textureCube(skybox, skypos).rgb;
                    sky = sky * light_model.w + (vec3(1, 1, 1) - sky * light_model.w) * getSun(skypos) * light_model.z;

                    out_colour = vec4(clr + (vec3(1, 1, 1) - clr) * sky, 1);
                }
            ";
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddAttribute("in_vertex", 2);

            if (_sVB == null) {
                _sVB = new VertexBuffer(2);
                _sVB.SetData(new float[] { -1f, -1f, 1f, -1f, 1f, 1f, -1f, 1f });
            }
        }

        public void BeginBatch()
        {
            _sVB.Begin(this);
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            if (DepthTest) {
                GL.Enable(EnableCap.DepthTest);
            }
        }

        public void EndBatch()
        {
            _sVB.End();
        }

        public void Render(Sphere sphere)
        {
            SetUniform("sphere", new Vector4(sphere.Position, sphere.Radius));
            if (Camera is SphereCamera) {
                SetTexture("skybox", ((SphereCamera) Camera).SkyBox);
            }

            SetUniform("light_model", new Vector4(sphere.Ambient, sphere.Diffuse, sphere.Specular, sphere.Reflect));
            SetUniform("colour", sphere.Colour);

            _sVB.Render();
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            if (DepthTest) {
                GL.Disable(EnableCap.DepthTest);
            }
        }
    }
}
