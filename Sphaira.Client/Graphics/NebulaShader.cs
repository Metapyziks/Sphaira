using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public NebulaShader()
        {
            var vert = new ShaderBuilder(ShaderType.VertexShader, false);
            vert.AddUniform(ShaderVarType.Mat4, "view");
            vert.AddUniform(ShaderVarType.Mat4, "proj");
            vert.AddUniform(ShaderVarType.Vec3, "camera");
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

            var frag = new ShaderBuilder(ShaderType.FragmentShader, false, vert);
            frag.AddUniform(ShaderVarType.Mat4, "view");
            frag.AddUniform(ShaderVarType.Mat4, "proj");
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

            VertexSource = vert.Generate();
            FragmentSource = frag.Generate();

            BeginMode = BeginMode.Quads;

            Create();
        }

        protected override void OnCreate()
        {
            AddUniform("view");
            AddUniform("proj");
            AddUniform("camera");

            AddAttribute("in_vertex", 2);

            AddUniform("nebula");

            AddUniform("colour");

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
            if (Camera != null) {
                var viewMat = Camera.ViewMatrix;
                var projMat = Camera.PerspectiveMatrix;
                SetUniform("view", ref viewMat);
                SetUniform("proj", ref projMat);
                SetUniform("camera", Camera.Position);
            }

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
            GL.Disable(EnableCap.Blend);
        }
    }
}
