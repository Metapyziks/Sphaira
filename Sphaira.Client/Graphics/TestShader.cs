using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Shaders;
using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    public class TestShader : ShaderProgram3D<SphereCamera>
    {
        public TestShader()
        {
            var vert = new ShaderBuilder(ShaderType.VertexShader, false);
            vert.AddUniform(ShaderVarType.Mat4, "vp_matrix");
            vert.AddUniform(ShaderVarType.Float, "scale");
            vert.AddAttribute(ShaderVarType.Vec3, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec3, "var_normal");
            vert.Logic = @"
                void main(void)
                {
                    var_normal = in_vertex;

                    gl_Position = vp_matrix * vec4(in_vertex * scale, 1);
                }
            ";

            var frag = new ShaderBuilder(ShaderType.FragmentShader, false, vert);
            frag.AddUniform(ShaderVarType.Vec4, "color");
            frag.AddUniform(ShaderVarType.Vec3, "sun");
            frag.AddUniform(ShaderVarType.Float, "scale");
            frag.FragOutIdentifier = "out_colour";
            frag.Logic = @"
                void main(void)
                {
                    vec3 norm = normalize(var_normal);

                    float mult = length(norm - var_normal) * 512.0 + 0.5;
                    float light = dot(norm, -normalize(sun)) * 0.4 + 0.6;

                    out_colour = vec4(color.rgb * mult * light, 1.0);
                }
            ";

            VertexSource = vert.Generate();
            FragmentSource = frag.Generate();

            BeginMode = BeginMode.Triangles;

            Create();
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddUniform("scale");
            AddUniform("color");
            AddUniform("sun");

            AddAttribute("in_vertex", 3);
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            GL.CullFace(CullFaceMode.Front);
        }

        protected override void OnEnd()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
        }
    }
}
