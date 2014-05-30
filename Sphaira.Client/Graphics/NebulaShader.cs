using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using OpenTKTK.Scene;
using OpenTKTK.Shaders;
using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    public class Nebula
    {
        public Vector3 Position { get; set; }

        public float Size { get; set; }

        public Color4 Colour { get; set; }

        public Nebula(Vector3 pos, float size, Color4 colour)
        {
            Position = pos;
            Size = size;
            Colour = colour;
        }
    }

    public class NebulaShader : ShaderProgram3D<Camera>
    {
        private static VertexBuffer _sVB;
        private static NebulaShader _sInstance;

        public static NebulaShader Instance
        {
            get
            {
                if (_sInstance == null) {
                    _sInstance = new NebulaShader();
                }

                return _sInstance;
            }
        }

        public NebulaShader()
        {
            PrimitiveType = PrimitiveType.Quads;
        }

        protected override void ConstructVertexShader(ShaderBuilder vert)
        {
            base.ConstructVertexShader(vert);

            vert.AddUniform(ShaderVarType.Vec4, "nebula");
            vert.AddAttribute(ShaderVarType.Vec2, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec2, "var_texcoord");
            vert.Logic = @"
                void main(void)
                {
                    vec3 diff = camera - nebula.xyz;
                    vec3 up = normalize(cross(diff, (view * vec4(0, 0, 1, 0)).xyz)) * nebula.w * 0.5;
                    vec3 right = normalize(cross(diff, up)) * nebula.w * 0.5;

                    var_texcoord = in_vertex;

                    gl_Position = proj * view * vec4(nebula.xyz + in_vertex.x * right + in_vertex.y * up, 1);
                }
            ";
        }

        protected override void ConstructFragmentShader(ShaderBuilder frag)
        {
            base.ConstructFragmentShader(frag);

            frag.AddUniform(ShaderVarType.Vec4, "colour");
            frag.Logic = @"
                void main(void)
                {
                    const float pi = 3.14159265359;

                    float dist = length(var_texcoord);
                    if (dist > 1) discard;

                    out_colour = vec4(colour.rgb, colour.a * cos(dist * pi));
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

            GL.Enable(EnableCap.Blend);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }

        public void EndBatch()
        {
            _sVB.End();
        }

        public void Render(Nebula nebula)
        {
            SetUniform("nebula", new Vector4(nebula.Position, nebula.Size));
            SetUniform("colour", nebula.Colour);

            _sVB.Render();
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            GL.Disable(EnableCap.Blend);
        }
    }
}
