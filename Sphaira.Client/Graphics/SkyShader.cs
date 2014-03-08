﻿using System;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Scene;
using OpenTKTK.Shaders;
using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    public class SkyShader : ShaderProgram3D<Camera>
    {
        internal static readonly String GetSunSource = @"
            float getSun(vec3 pos)
            {
                pos = normalize(pos);

                vec3 sundir = normalize(sun - camera);
                vec3 lookdir = pos - sundir;
                vec3 up = cross(vec3(0, 1, 0), sundir);
                vec3 right = cross(up, sundir);

                float mag = dot(sundir, pos);
                float ang = atan(dot(lookdir, up), dot(lookdir, right));
                float mul = sin(ang * 15 + time) * 0.01 + sin(ang * 7 - time * 3) * 0.01 + 0.4;

                return max(0, min(1, pow(mag, 512) + pow(mag, 8) * mul));
            }
        ";

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
        private static SkyShader _sInstance;

        public static SkyShader Instance
        {
            get
            {
                if (_sInstance == null) {
                    _sInstance = new SkyShader();
                }

                return _sInstance;
            }
        }

        public SkyShader()
        {
            BeginMode = BeginMode.Quads;
        }

        protected override void ConstructVertexShader(ShaderBuilder vert)
        {
            base.ConstructVertexShader(vert);

            vert.AddAttribute(ShaderVarType.Vec3, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec3, "var_texcoord");
            vert.Logic = @"
                void main(void)
                {
                    vec4 pos = proj * view * vec4(in_vertex, 0);

                    gl_Position = pos.xyww;
                    var_texcoord = in_vertex;
                }
            ";
        }

        protected override void ConstructFragmentShader(ShaderBuilder frag)
        {
            base.ConstructFragmentShader(frag);

            frag.AddUniform(ShaderVarType.SamplerCube, "skybox");
            frag.AddUniform(ShaderVarType.Vec3, "sun");
            frag.AddUniform(ShaderVarType.Float, "time");
            frag.Logic = GetSunSource + @"
                void main(void)
                {
                    vec3 sky = textureCube(skybox, var_texcoord).rgb;
                    out_colour = vec4(sky + (vec3(1, 1, 1) - sky) * getSun(var_texcoord), 1);
                }
            ";
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddAttribute("in_vertex", 3);

            if (_sVB == null) {
                _sVB = new VertexBuffer(3);
                _sVB.SetData(_sVerts);
            }
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            if (Camera is SphereCamera) {
                SetTexture("skybox", ((SphereCamera) Camera).SkyBox);
            }
        }

        public void Render()
        {
            _sVB.Begin(this);
            _sVB.Render();
            _sVB.End();
        }
    }
}
