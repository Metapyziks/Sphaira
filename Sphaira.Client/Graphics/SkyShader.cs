using OpenTK.Graphics.OpenGL;

using OpenTKTK.Shaders;
using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    public class SkyShader : ShaderProgram3D<SphereCamera>
    {
        private const float BoxSize = 1f;

        private static readonly float[] _sVerts = new float[] {
            -BoxSize, -BoxSize, -BoxSize, /**/ -BoxSize, +BoxSize, -BoxSize, /**/ -BoxSize, +BoxSize, +BoxSize, /**/ -BoxSize, -BoxSize, +BoxSize, // Left
            +BoxSize, -BoxSize, -BoxSize, /**/ +BoxSize, +BoxSize, -BoxSize, /**/ +BoxSize, +BoxSize, +BoxSize, /**/ +BoxSize, -BoxSize, +BoxSize, // Right
            -BoxSize, -BoxSize, -BoxSize, /**/ +BoxSize, -BoxSize, -BoxSize, /**/ +BoxSize, -BoxSize, +BoxSize, /**/ -BoxSize, -BoxSize, +BoxSize, // Bottom
            -BoxSize, +BoxSize, -BoxSize, /**/ +BoxSize, +BoxSize, -BoxSize, /**/ +BoxSize, +BoxSize, +BoxSize, /**/ -BoxSize, +BoxSize, +BoxSize, // Top
            -BoxSize, -BoxSize, -BoxSize, /**/ +BoxSize, -BoxSize, -BoxSize, /**/ +BoxSize, +BoxSize, -BoxSize, /**/ -BoxSize, +BoxSize, -BoxSize, // Front
            -BoxSize, -BoxSize, +BoxSize, /**/ +BoxSize, -BoxSize, +BoxSize, /**/ +BoxSize, +BoxSize, +BoxSize, /**/ -BoxSize, +BoxSize, +BoxSize, // Back
        };

        private static VertexBuffer _sVB;

        public SkyShader()
        {
            ShaderBuilder vert = new ShaderBuilder(ShaderType.VertexShader, false);
            vert.AddUniform(ShaderVarType.Mat4, "vp_matrix");
            vert.AddAttribute(ShaderVarType.Vec3, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec3, "var_texcoord");
            vert.Logic = @"
                void main(void)
                {
                    vec4 pos = vp_matrix * vec4(in_vertex, 0.0);
                    gl_Position = pos.xyww;
                    var_texcoord = in_vertex;
                }
            ";

            ShaderBuilder frag = new ShaderBuilder(ShaderType.FragmentShader, false, vert);
            frag.AddUniform(ShaderVarType.SamplerCube, "skybox");
            frag.Logic = @"
                void main(void)
                {
                    out_colour = textureCube(skybox, var_texcoord);
                }
            ";

            VertexSource = vert.Generate();
            FragmentSource = frag.Generate();

            BeginMode = BeginMode.Quads;

            Create();
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddAttribute("in_vertex", 3);

            AddTexture("skybox");

            if (_sVB == null) {
                _sVB = new VertexBuffer(3);
                _sVB.SetData(_sVerts);
            }
        }
        protected override void OnBegin()
        {
            base.OnBegin();

            SetTexture("skybox", Camera.SkyBox);
        }

        public void Render()
        {
            _sVB.Begin(this);
            _sVB.Render();
            _sVB.End();
        }
    }
}
