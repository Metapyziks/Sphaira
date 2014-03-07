﻿using System;
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
        public NebulaShader()
        {
            var vert = new ShaderBuilder(ShaderType.VertexShader, false);
            vert.AddUniform(ShaderVarType.Mat4, "view");
            vert.AddUniform(ShaderVarType.Mat4, "proj");
            vert.AddUniform(ShaderVarType.Vec3, "camera");
            vert.AddUniform(ShaderVarType.Vec4, "nebula");
            vert.AddAttribute(ShaderVarType.Vec2, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec3, "var_position");
            vert.AddVarying(ShaderVarType.Vec2, "var_texcoord");
            vert.Logic = @"
                void main(void)
                {
                    vec3 diff = camera - nebula.xyz;
                    vec3 up = normalize(cross(diff, (view * vec4(0, 0, 1, 0)).xyz)) * nebula.w * 0.5;
                    vec3 right = normalize(cross(diff, up)) * nebula.w * 0.5;

                    vec3 pos = nebula.xyz + in_vertex.x * right + in_vertex.y * up;

                    var_position = pos;
                    var_texcoord = in_vertex;

                    gl_Position = proj * view * vec4(pos, 1);
                }
            ";

            var frag = new ShaderBuilder(ShaderType.FragmentShader, false, vert);
            frag.AddUniform(ShaderVarType.Mat4, "view");
            frag.AddUniform(ShaderVarType.Mat4, "proj");
            frag.AddUniform(ShaderVarType.Vec4, "colour");
            frag.Logic = @"
                vec3 mod289(vec3 x)
                {
                    return x - floor(x * (1.0 / 289.0)) * 289.0;
                }

                vec4 mod289(vec4 x)
                {
                    return x - floor(x * (1.0 / 289.0)) * 289.0;
                }

                vec4 permute(vec4 x)
                {
                    return mod289(((x*34.0)+1.0)*x);
                }

                vec4 taylorInvSqrt(vec4 r)
                {
                    return 1.79284291400159 - 0.85373472095314 * r;
                }

                vec3 fade(vec3 t) {
                    return t*t*t*(t*(t*6.0-15.0)+10.0);
                }

                float cnoise(vec3 P)
                {
                    vec3 Pi0 = floor(P); // Integer part for indexing
                    vec3 Pi1 = Pi0 + vec3(1.0); // Integer part + 1
                    Pi0 = mod289(Pi0);
                    Pi1 = mod289(Pi1);
                    vec3 Pf0 = fract(P); // Fractional part for interpolation
                    vec3 Pf1 = Pf0 - vec3(1.0); // Fractional part - 1.0
                    vec4 ix = vec4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
                    vec4 iy = vec4(Pi0.yy, Pi1.yy);
                    vec4 iz0 = Pi0.zzzz;
                    vec4 iz1 = Pi1.zzzz;

                    vec4 ixy = permute(permute(ix) + iy);
                    vec4 ixy0 = permute(ixy + iz0);
                    vec4 ixy1 = permute(ixy + iz1);

                    vec4 gx0 = ixy0 * (1.0 / 7.0);
                    vec4 gy0 = fract(floor(gx0) * (1.0 / 7.0)) - 0.5;
                    gx0 = fract(gx0);
                    vec4 gz0 = vec4(0.5) - abs(gx0) - abs(gy0);
                    vec4 sz0 = step(gz0, vec4(0.0));
                    gx0 -= sz0 * (step(0.0, gx0) - 0.5);
                    gy0 -= sz0 * (step(0.0, gy0) - 0.5);

                    vec4 gx1 = ixy1 * (1.0 / 7.0);
                    vec4 gy1 = fract(floor(gx1) * (1.0 / 7.0)) - 0.5;
                    gx1 = fract(gx1);
                    vec4 gz1 = vec4(0.5) - abs(gx1) - abs(gy1);
                    vec4 sz1 = step(gz1, vec4(0.0));
                    gx1 -= sz1 * (step(0.0, gx1) - 0.5);
                    gy1 -= sz1 * (step(0.0, gy1) - 0.5);

                    vec3 g000 = vec3(gx0.x,gy0.x,gz0.x);
                    vec3 g100 = vec3(gx0.y,gy0.y,gz0.y);
                    vec3 g010 = vec3(gx0.z,gy0.z,gz0.z);
                    vec3 g110 = vec3(gx0.w,gy0.w,gz0.w);
                    vec3 g001 = vec3(gx1.x,gy1.x,gz1.x);
                    vec3 g101 = vec3(gx1.y,gy1.y,gz1.y);
                    vec3 g011 = vec3(gx1.z,gy1.z,gz1.z);
                    vec3 g111 = vec3(gx1.w,gy1.w,gz1.w);

                    vec4 norm0 = taylorInvSqrt(vec4(dot(g000, g000), dot(g010, g010), dot(g100, g100), dot(g110, g110)));
                    g000 *= norm0.x;
                    g010 *= norm0.y;
                    g100 *= norm0.z;
                    g110 *= norm0.w;
                    vec4 norm1 = taylorInvSqrt(vec4(dot(g001, g001), dot(g011, g011), dot(g101, g101), dot(g111, g111)));
                    g001 *= norm1.x;
                    g011 *= norm1.y;
                    g101 *= norm1.z;
                    g111 *= norm1.w;

                    float n000 = dot(g000, Pf0);
                    float n100 = dot(g100, vec3(Pf1.x, Pf0.yz));
                    float n010 = dot(g010, vec3(Pf0.x, Pf1.y, Pf0.z));
                    float n110 = dot(g110, vec3(Pf1.xy, Pf0.z));
                    float n001 = dot(g001, vec3(Pf0.xy, Pf1.z));
                    float n101 = dot(g101, vec3(Pf1.x, Pf0.y, Pf1.z));
                    float n011 = dot(g011, vec3(Pf0.x, Pf1.yz));
                    float n111 = dot(g111, Pf1);

                    vec3 fade_xyz = fade(Pf0);
                    vec4 n_z = mix(vec4(n000, n100, n010, n110), vec4(n001, n101, n011, n111), fade_xyz.z);
                    vec2 n_yz = mix(n_z.xy, n_z.zw, fade_xyz.y);
                    float n_xyz = mix(n_yz.x, n_yz.y, fade_xyz.x); 
                    return 2.2 * n_xyz;
                }

                float nebula(vec3 pos, vec2 tex)
                {
                    float freq = 0.3;
                    float val = 0.5;
                    
                    val += 0.5 * cnoise(pos * freq);
                    val += 0.25 * cnoise(pos * freq * 2.0);
                    val += 0.125 * cnoise(pos * freq * 4.0);

                    return clamp(val * max(0.0, 1.0 - length(tex)), 0.0, 1.0);
                }

                void main(void)
                {
                    out_colour = vec4(colour.rgb, colour.a * nebula(var_position, var_texcoord));
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

        public void Render(Nebula nebula)
        {
            SetUniform("nebula", new Vector4(nebula.Position, nebula.Size));
            SetUniform("colour", nebula.Colour);

            Begin(true);
            Render(new float[] { -1f, -1f, 1f, -1f, 1f, 1f, -1f, 1f });
            End();
        }

        protected override void OnEnd()
        {
            GL.Disable(EnableCap.Blend);
        }
    }
}
