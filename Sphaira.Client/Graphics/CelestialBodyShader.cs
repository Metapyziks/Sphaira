using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using OpenTKTK.Scene;
using OpenTKTK.Shaders;
using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    abstract class CelestialBodyShader<T> : ShaderProgram3D<Camera>
        where T : CelestialBody
    {
        private static VertexBuffer _sVB;

        public bool DepthTest { get; set; }

        public bool Blend { get; set; }

        public CelestialBodyShader()
        {
            DepthTest = true;
            PrimitiveType = PrimitiveType.Quads;
        }

        protected override void ConstructVertexShader(ShaderBuilder vert)
        {
            base.ConstructVertexShader(vert);

            vert.AddUniform(ShaderVarType.Vec4, "body");
            vert.AddAttribute(ShaderVarType.Vec2, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec3, "var_position");
            vert.Logic = @"
                void main(void)
                {
                    float r = body.w;
                    vec3 cam = camera - body.xyz;
                    float dist = length(cam);
                    float hyp = sqrt(dist * dist - r * r);
                    float ang = atan(r / hyp);
                    float opp = hyp * sin(ang);
                    float dif = sqrt(r * r - opp * opp);

                    vec3 center = normalize(cam) * dif;
                    vec3 up = normalize(cross(cam, (view * vec4(0, 0, 1, 0)).xyz)) * opp;
                    vec3 right = normalize(cross(cam, up)) * opp;

                    var_position = vec3(center + in_vertex.x * right + in_vertex.y * up) / r;

                    gl_Position = proj * view * vec4(body.xyz + center + in_vertex.x * right + in_vertex.y * up, 1);
                }
            ";
        }

        protected override void ConstructFragmentShader(ShaderBuilder frag)
        {
            base.ConstructFragmentShader(frag);

            frag.AddUniform(ShaderVarType.Vec4, "body");
            frag.Logic = GetRenderFuncSource() + @"
                void main(void)
                {
                    float r = body.w;
                    vec3 cam = camera - body.xyz;
                    float len2 = dot(var_position, var_position);

                    if (len2 > 1) discard;

                    vec3 l = normalize(cam / r - var_position);

                    float b = dot(l, var_position);
                    float d = -b + sqrt(b * b - len2 + 1);

                    vec3 pos = (var_position + l * d) * r;
                    vec4 fin = proj * view * vec4(pos + body.xyz, 1);

                    gl_FragDepth = (fin.z / fin.w + 1) / 2;

                    out_colour = render(pos, cam);
                }
            ";
        }

        protected abstract String GetRenderFuncSource();

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

            if (Blend) {
                GL.Enable(EnableCap.Blend);

                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            }
        }

        public void EndBatch()
        {
            _sVB.End();
        }

        public void Render(T body)
        {
            OnRender(body);

            _sVB.Render();
        }

        protected virtual void OnRender(T body)
        {
            SetUniform("body", new Vector4(body.Position, body.Radius));
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            if (DepthTest) {
                GL.Disable(EnableCap.DepthTest);
            }

            if (Blend) {
                GL.Disable(EnableCap.Blend);
            }
        }
    }
}
