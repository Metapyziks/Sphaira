﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Shaders;
using OpenTKTK.Utils;
using Sphaira.Shared.Geometry;

namespace Sphaira.Client.Graphics
{
    public class SphereShader : ShaderProgram3D<SphereCamera>
    {
        public SphereShader()
        {
            var vert = new ShaderBuilder(ShaderType.VertexShader, false);
            vert.AddUniform(ShaderVarType.Mat4, "view");
            vert.AddUniform(ShaderVarType.Mat4, "proj");
            vert.AddUniform(ShaderVarType.Vec3, "camera");
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

            var frag = new ShaderBuilder(ShaderType.FragmentShader, false, vert);
            frag.AddUniform(ShaderVarType.Mat4, "view");
            frag.AddUniform(ShaderVarType.Mat4, "proj");
            frag.AddUniform(ShaderVarType.Vec3, "camera");
            frag.AddUniform(ShaderVarType.Vec4, "sphere");
            frag.AddUniform(ShaderVarType.Vec3, "sun");
            frag.AddUniform(ShaderVarType.SamplerCube, "skybox");
            frag.FragOutIdentifier = "out_colour";
            frag.Logic = @"
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
                    vec3 sundir = -normalize(sun);
                    float light = max(1.0 / 32.0, dot(normal, sundir));
                    float spclr = pow(max(0, dot(reflect(sundir, normal), lookdir)), 16) * 0.75;
                    float check = ((int(pos.x) + int(pos.y) + int(pos.z)) & 1) * 0.125 + 0.5;
                    vec3 clr = check * light;

                    clr += (textureCube(skybox, normalize(reflect(lookdir, normal))).rgb - clr) * 0.5;

                    out_colour = vec4(clr + (vec3(1, 1, 1) - clr) * spclr, 1);
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

            AddUniform("sphere");

            AddUniform("sun");

            AddTexture("skybox");
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

            GL.Enable(EnableCap.DepthTest);
        }

        public void Render(Sphere sphere)
        {
            SetUniform("sphere", new Vector4(sphere.Position, sphere.Radius));
            SetTexture("skybox", Camera.SkyBox);

            Begin(true);
            Render(new float[] { -1f, -1f, 1f, -1f, 1f, 1f, -1f, 1f });
            End();
        }

        protected override void OnEnd()
        {
            GL.Disable(EnableCap.DepthTest);
        }
    }
}
