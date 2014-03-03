﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Shaders;
using OpenTKTK.Utils;
using Sphaira.Client.Geometry;

namespace Sphaira.Client.Graphics
{
    public class EllipseShader : ShaderProgram3D<SphereCamera>
    {
        public EllipseShader()
        {
            var vert = new ShaderBuilder(ShaderType.VertexShader, false);
            vert.AddUniform(ShaderVarType.Mat4, "view");
            vert.AddUniform(ShaderVarType.Mat4, "proj");
            vert.AddUniform(ShaderVarType.Vec3, "camera");
            vert.AddUniform(ShaderVarType.Float, "radius");
            vert.AddAttribute(ShaderVarType.Vec2, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec3, "var_position");
            vert.Logic = @"
                void main(void)
                {
                    float dist = length(camera);
                    float hyp = sqrt(dist * dist - radius * radius);
                    float ang = atan(radius / hyp);
                    float opp = hyp * sin(ang);
                    float dif = sqrt(radius * radius - opp * opp);

                    vec3 center = normalize(camera) * dif;
                    vec3 up = normalize(cross(camera, (view * vec4(0, 0, 1, 0)).xyz)) * opp;
                    vec3 right = normalize(cross(camera, up)) * opp;

                    var_position = vec3(center + in_vertex.x * right + in_vertex.y * up) / radius;

                    gl_Position = proj * view * vec4(center + in_vertex.x * right + in_vertex.y * up, 1);
                }
            ";

            var frag = new ShaderBuilder(ShaderType.FragmentShader, false, vert);
            frag.AddUniform(ShaderVarType.Float, "radius");
            frag.AddUniform(ShaderVarType.Vec3, "camera");
            frag.FragOutIdentifier = "out_colour";
            frag.Logic = @"
                void main(void)
                {
                    float len2 = dot(var_position, var_position);

                    if (len2 > 1) discard;

                    vec3 l = normalize(camera / radius - var_position);

                    float b = dot(l, var_position);
                    float d = -b + sqrt(b * b - len2 + 1);

                    vec3 pos = (var_position + l * d) * radius;

                    out_colour = vec4(int(pos.x) & 1, int(pos.y) & 1, int(pos.z) & 1, 1);
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

            AddAttribute("in_vertex", 2);

            AddUniform("camera");
            AddUniform("radius");
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

            //GL.Enable(EnableCap.DepthTest);
        }

        public void Render(Sphere sphere)
        {
            SetUniform("radius", sphere.Radius);

            Begin(true);
            Render(new float[] { -1f, -1f, 1f, -1f, 1f, 1f, -1f, 1f });
            End();
        }

        protected override void OnEnd()
        {
            //GL.Disable(EnableCap.DepthTest);
        }
    }
}
