using System;
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

        private BitmapTexture2D _frameTexture;

        public BitmapTexture2D FrameTexture
        {
            get { return _frameTexture; }
            set
            {
                _frameTexture = value;
                SetTexture("frame", value);
            }
        }

        protected override void ConstructVertexShader(ShaderBuilder vert)
        {
            base.ConstructVertexShader(vert);

            vert.AddAttribute(ShaderVarType.Vec2, "in_vertex");
            vert.AddVarying(ShaderVarType.Vec2, "var_texcoord");
            vert.Logic = @"
                void main(void)
                {
                    var_texcoord = vec2(in_vertex.x, 1 - in_vertex.y);
                    gl_Position = in_vertex * screen_resolution;
                }
            ";
        }

        protected override void ConstructFragmentShader(ShaderBuilder frag)
        {
            base.ConstructFragmentShader(frag);

            frag.AddUniform(ShaderVarType.Sampler2D, "frame");
            frag.Logic = @"
                void main(void)
                {
                    gl_FragColor = texture2D(frame, var_texcoord);
                }
            ";
        }

        public PostProcessShader()
        {
            BeginMode = BeginMode.Quads;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddAttribute("in_vertex", 2);

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
