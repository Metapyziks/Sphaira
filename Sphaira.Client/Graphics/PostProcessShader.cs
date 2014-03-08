﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Scene;
using OpenTKTK.Shaders;
using OpenTKTK.Textures;
using OpenTKTK.Utils;

namespace Sphaira.Client.Graphics
{
    public class PostProcessShader : ShaderProgram2D
    {
        private static VertexBuffer _sVB;
        private static PostProcessShader _sInstance;

        public static PostProcessShader Instance
        {
            get
            {
                if (_sInstance == null) {
                    _sInstance = new PostProcessShader();
                }

                return _sInstance;
            }
        }

        private BitmapTexture2D _frameTexture;

        public BitmapTexture2D FrameTexture
        {
            get { return _frameTexture; }
            set
            {
                _frameTexture = value;
                SetTexture("frametex", value);
            }
        }

        public PostProcessShader()
        {
            var vert = new ShaderBuilder(ShaderType.VertexShader, true);
            vert.AddAttribute(ShaderVarType.Vec2, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec2, "var_texcoord");
            vert.Logic = @"
                void main(void)
                {
                    var_texcoord = vec2(in_vertex.x, 1 - in_vertex.y);
                    gl_Position = in_vertex * screen_resolution;
                }
            ";

            var frag = new ShaderBuilder(ShaderType.FragmentShader, true, vert);
            frag.AddUniform(ShaderVarType.Sampler2D, "frametex");
            frag.AddUniform(ShaderVarType.Vec2, "screen_resolution");
            frag.Logic = @"
                void main(void)
                {
                    const float FXAA_SPAN_MAX = 8.0;
                    const float FXAA_REDUCE_MUL = 1.0 / 8.0;
                    const float FXAA_REDUCE_MIN = 1.0 / 128.0;

                    vec2 rcpRes = vec2(1 / screen_resolution.x, 1 / screen_resolution.y);

                    vec3 rgbNW = texture2D(frametex, var_texcoord + (vec2(-1.0, -1.0) * rcpRes)).xyz;
                    vec3 rgbNE = texture2D(frametex, var_texcoord + (vec2( 1.0, -1.0) * rcpRes)).xyz;
                    vec3 rgbSW = texture2D(frametex, var_texcoord + (vec2(-1.0,  1.0) * rcpRes)).xyz;
                    vec3 rgbSE = texture2D(frametex, var_texcoord + (vec2( 1.0,  1.0) * rcpRes)).xyz;
                    vec3 rgbM = texture2D(frametex, var_texcoord).xyz;

                    vec3 luma = vec3(0.299, 0.587, 0.114);

                    float lumaNW = dot(rgbNW, luma);
                    float lumaNE = dot(rgbNE, luma);
                    float lumaSW = dot(rgbSW, luma);
                    float lumaSE = dot(rgbSE, luma);
                    float lumaM  = dot(rgbM,  luma);
        
                    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
                    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
        
                    vec2 dir;
                    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
                    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));
        
                    float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) *
                        (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
          
                    float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
        
                    dir = min(vec2(FXAA_SPAN_MAX, FXAA_SPAN_MAX),
                        max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),
                        dir * rcpDirMin)) * rcpRes;
                
                    vec3 rgbA = 0.5 * (
                        texture2D(frametex, var_texcoord.xy + dir * (1.0 / 3.0 - 0.5)).xyz +
                        texture2D(frametex, var_texcoord.xy + dir * (2.0 / 3.0 - 0.5)).xyz);

                    vec3 rgbB = rgbA * 0.5 + 0.25 * (
                        texture2D(frametex, var_texcoord.xy + dir * -0.5).xyz +
                        texture2D(frametex, var_texcoord.xy + dir *  0.5).xyz);

                    float lumaB = dot(rgbB, luma);

                    if ((lumaB < lumaMin) || (lumaB > lumaMax)) {
                        gl_FragColor = vec4(rgbA, 1);
                    } else {
                        gl_FragColor = vec4(rgbB, 1);
                    }
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

            AddAttribute("in_vertex", 2);

            AddTexture("frametex");

            if (_sVB == null) {
                _sVB = new VertexBuffer(2);
                _sVB.SetData(new float[] { 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f });
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
