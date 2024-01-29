using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

using DMG;

namespace OglRenderer
{
    // Uses OpenTK to create a window and OpenGL based rendering. This is a more basic window (no debug window functionality) than the WinFormsDmgRenderer but it is cross platform and has been tested on Windows and Mac.

    public class Window : GameWindow
    {
        DmgSystem dmg;

        // Because we're adding a texture, we modify the vertex array to include texture coordinates.
        // Texture coordinates range from 0.0 to 1.0, with (0.0, 0.0) representing the bottom left, and (1.0, 1.0) representing the top right.
        // The new layout is three floats to create a vertex, then two floats to create the coordinates.
        private readonly float[] _vertices =
        {
            // Position         Texture coordinates
             0.5f,  0.5f, 0.0f, 1.0f, 1.0f, // top right
             0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // bottom right
            -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, // bottom left
            -0.5f,  0.5f, 0.0f, 0.0f, 1.0f  // top left
        };

        private readonly uint[] _indices =
        {
            0, 1, 3,
            1, 2, 3
        };

        private int _elementBufferObject;

        private int _vertexBufferObject;

        private int _vertexArrayObject;

        private Shader _shader;

        // For documentation on this, check Texture.cs.
        private Texture _texture;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // Update Fast as possible 
            UpdateFrequency = 0f;

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            _elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            // The shaders have been modified to include the texture coordinates, check them out after finishing the OnLoad function.
            _shader = new Shader("../../../Shaders/shader.vert", "../../../Shaders/shader.frag");
            _shader.Use();

            // Because there's now 5 floats between the start of the first vertex and the start of the second,
            // we modify the stride from 3 * sizeof(float) to 5 * sizeof(float).
            // This will now pass the new vertex array to the buffer.
            var vertexLocation = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            // Next, we also setup texture coordinates. It works in much the same way.
            // We add an offset of 3, since the texture coordinates comes after the position data.
            // We also change the amount of data to 2 because there's only 2 floats for texture coordinates.
            var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            _texture = Texture.Create();
            _texture.Use(TextureUnit.Texture0);


            Reset("../../../../Roms/Games/Legend of Zelda, The - Link's Awakening (U) (V1.2).gb");
        }

        

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_vertexArrayObject);

            _texture.Use(TextureUnit.Texture0);
            _shader.Use();

            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }


        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);


            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (input.IsKeyDown(Keys.Up)) dmg.pad.UpdateKeyState(Joypad.GbKey.Up, true);
            else if (input.IsKeyDown(Keys.Down)) dmg.pad.UpdateKeyState(Joypad.GbKey.Down, true);
            else if (input.IsKeyDown(Keys.Left)) dmg.pad.UpdateKeyState(Joypad.GbKey.Left, true);
            else if (input.IsKeyDown(Keys.Right)) dmg.pad.UpdateKeyState(Joypad.GbKey.Right, true);
            else if (input.IsKeyDown(Keys.Z)) dmg.pad.UpdateKeyState(Joypad.GbKey.B, true);
            else if (input.IsKeyDown(Keys.X)) dmg.pad.UpdateKeyState(Joypad.GbKey.A, true);
            else if (input.IsKeyDown(Keys.Enter)) dmg.pad.UpdateKeyState(Joypad.GbKey.Start, true);
            else if (input.IsKeyDown(Keys.Backspace)) dmg.pad.UpdateKeyState(Joypad.GbKey.Select, true);


            if (input.IsKeyReleased(Keys.Up)) dmg.pad.UpdateKeyState(Joypad.GbKey.Up, false);
            else if (input.IsKeyReleased(Keys.Down)) dmg.pad.UpdateKeyState(Joypad.GbKey.Down, false);
            else if (input.IsKeyReleased(Keys.Left)) dmg.pad.UpdateKeyState(Joypad.GbKey.Left, false);
            else if (input.IsKeyReleased(Keys.Right)) dmg.pad.UpdateKeyState(Joypad.GbKey.Right, false);
            else if (input.IsKeyReleased(Keys.Z)) dmg.pad.UpdateKeyState(Joypad.GbKey.B, false);
            else if (input.IsKeyReleased(Keys.X)) dmg.pad.UpdateKeyState(Joypad.GbKey.A, false);
            else if (input.IsKeyReleased(Keys.Enter)) dmg.pad.UpdateKeyState(Joypad.GbKey.Start, false);
            else if (input.IsKeyReleased(Keys.Backspace)) dmg.pad.UpdateKeyState(Joypad.GbKey.Select, false);


            if (dmg != null && dmg.PoweredOn)
            {
                // Step the emulator for an entire frame 
                while (frameDrawn == false)
                {
                    dmg.Step();
                }
                frameDrawn = false;
            }
        }


        bool frameDrawn = false;
        void Draw()
        {
            Texture.UpdateTexture(dmg.FrameBuffer);
            frameDrawn = true;
        }



        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
        }



        void Reset(string romFilename)
        {
            // Gameboy itself doesn't support reset so i see no reason to contrive one. Just create a fresh one 
            string rom = romFilename;
            dmg = new DmgSystem();
            dmg.OnFrame = () => this.Draw();          

            dmg.PowerOn(rom);
        }
    }
}