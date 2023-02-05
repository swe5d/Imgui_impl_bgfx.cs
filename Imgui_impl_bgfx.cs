// Derived from this Gist by pr0g:
//     https://gist.github.com/pr0g/aff79b71bf9804ddb03f39ca7c0c3bbb

// Notice:
//      this code is only example and still has some TODOs,
//      so you better to use it only after your own enhancement.
// 
// TODO: 
//      this code do not contains content and functions of window backend like glfw.
//      it is just a renderer. so if you want to use your mouse and keyboard to interact 
//      with Imgui widget, you have to implement files like "Imgui_impl_glfw" and "Imgui_impl_sdl"

using Bgfx;
using static Bgfx.bgfx;

namespace ImGuiNET
{
    public class Imgui_impl_bgfx
    {
        // Data
        static uint g_View = 255;
        static bgfx.TextureHandle g_FontTexture;
        static bgfx.ProgramHandle g_ShaderHandle;
        static bgfx.UniformHandle g_AttribLocationTex;
        static bgfx.VertexLayout g_VertexLayout;


        public unsafe static void RenderDrawLists(ImDrawDataPtr draw_data)
        {
            // Avoid rendering when minimized, scale coordinates for retina displays
            // (screen coordinates != framebuffer coordinates)
            var io = ImGui.GetIO();
            int fb_width = (int)(io.DisplaySize.X * io.DisplayFramebufferScale.X);
            int fb_height = (int)(io.DisplaySize.Y * io.DisplayFramebufferScale.Y);
            if (fb_width == 0 || fb_height == 0)
            {
                return;
            }
            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            // Setup render state: alpha-blending enabled, no face culling,
            // no depth testing, scissor enabled
            // TODO: below is the result of CPP code:
            //     uint64_t state =
            // BGFX_STATE_WRITE_RGB | BGFX_STATE_WRITE_A | BGFX_STATE_MSAA |
            // BGFX_STATE_BLEND_FUNC(
            //    BGFX_STATE_BLEND_SRC_ALPHA, BGFX_STATE_BLEND_INV_SRC_ALPHA);
            UInt64 state = 72057594144247823;

            var caps = bgfx.get_caps();
            var transVbSize = caps->limits.transientVbSize;
            var transIbSize = caps->limits.transientIbSize;

            // Setup viewport, orthographic projection matrix
            // TODO: please use your matrix lib to replace below hard-code matrix
            // below matrix is the result of
            // bx::mtxOrtho(ortho, 0.0f, 720, 480, 0.0f, 0.0f, 1000.0f, 0.0f, caps->homogeneousDepth);
            float[] ortho = new float[16]{0.00277777785f,
                                          0.00000000f,
                                          0.00000000f,
                                          0.00000000f,
                                          0.00000000f,
                                          -0.00416666688f,
                                          0.00000000f,
                                          0.00000000f,
                                          0.00000000f,
                                          0.00000000f,
                                          0.00100000005f,
                                          0.00000000f,
                                          -1.00000000f,
                                          1.00000000f,
                                          -0.00000000f,
                                          1.00000000f};
            fixed (float* p = &ortho[0])
            {
                bgfx.set_view_transform((ushort)g_View, null, p);
            }
            bgfx.set_view_rect((ushort)g_View, 0, 0, (ushort)fb_width, (ushort)fb_height);

            // Render command lists
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];

                bgfx.VertexBufferHandle vbh;
                bgfx.IndexBufferHandle ibh;
                uint numVertices = (uint)cmd_list.VtxBuffer.Size;
                uint numIndices = (uint)cmd_list.IdxBuffer.Size;

                // TODO: please use TransientVertexBuffer to replace VertexBufferHandle for better performance
                fixed (VertexLayout* g_VertexLayout_address = &g_VertexLayout)
                {
                    vbh =
                        bgfx.create_vertex_buffer(
                            bgfx.make_ref(cmd_list.VtxBuffer.Data.ToPointer(), 20 * numVertices),
                            g_VertexLayout_address,
                            (ushort)BufferFlags.None);
                    ibh =
                        bgfx.create_index_buffer(
                            bgfx.make_ref(cmd_list.IdxBuffer.Data.ToPointer(), 2 * numIndices),
                            (ushort)BufferFlags.None
                            );
                }

                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];

                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        ushort xx = (ushort)Math.Max(pcmd.ClipRect.X, 0.0f);
                        ushort yy = (ushort)Math.Max(pcmd.ClipRect.Y, 0.0f);
                        bgfx.set_scissor(
                            xx, yy, (ushort)(Math.Min(pcmd.ClipRect.Z, 65535.0f) - xx),
                            (ushort)(Math.Min(pcmd.ClipRect.W, 65535.0f) - yy));

                        bgfx.set_state(state, 0);
                        bgfx.TextureHandle texture;
                        texture.idx = (ushort)((Int64)pcmd.TextureId & 0xffff);
                        bgfx.set_texture(0, g_AttribLocationTex, texture, UInt32.MaxValue);
                        bgfx.set_vertex_buffer(0, vbh, 0, numVertices);
                        bgfx.set_index_buffer(ibh, 0, numIndices);
                        bgfx.submit((ushort)g_View, g_ShaderHandle, 0, (byte)DiscardFlags.All);
                    }
                }

                //End CmdList
                bgfx.destroy_vertex_buffer(vbh);
                bgfx.destroy_index_buffer(ibh);
            }
        }


        public unsafe static bool CreateFontsTexture()
        {
            // Build texture atlas
            var io = ImGui.GetIO();
            byte* pixels;
            int width, height;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

            // Upload texture to graphics system
            g_FontTexture = bgfx.create_texture_2d(
                (ushort)width, (ushort)height, false, 1, bgfx.TextureFormat.BGRA8,
                0, bgfx.copy(pixels, (uint)(width * height * 4)));

            // Store our identifier
            io.Fonts.TexID = (IntPtr)((void*)g_FontTexture.idx);

            return true;
        }

        public unsafe static bool CreateDeviceObjects()
        {
            bgfx.RendererType type = bgfx.get_renderer_type();
            ShaderHandle vs_shader, fs_shader;
            fixed (byte* p = EmbeddedImGuiShader.vs_ocornut_imgui_glsl)
            {
                
                vs_shader = bgfx.create_shader(bgfx.make_ref(p, (uint)EmbeddedImGuiShader.vs_ocornut_imgui_glsl.Length));
            }
            fixed (byte* p = EmbeddedImGuiShader.fs_ocornut_imgui_glsl)
            {
                fs_shader = bgfx.create_shader(bgfx.make_ref(p, (uint)EmbeddedImGuiShader.fs_ocornut_imgui_glsl.Length));
            }
            g_ShaderHandle = bgfx.create_program(
                vs_shader,
                fs_shader,
                true);
            fixed (VertexLayout* g_VertexLayout_address = &g_VertexLayout)
            {
                bgfx.vertex_layout_begin(g_VertexLayout_address, bgfx.RendererType.Noop);
                bgfx.vertex_layout_add(g_VertexLayout_address, bgfx.Attrib.Position, 2, bgfx.AttribType.Float, false, false);
                bgfx.vertex_layout_add(g_VertexLayout_address, bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float, false, false);
                bgfx.vertex_layout_add(g_VertexLayout_address, bgfx.Attrib.Color0, 4, bgfx.AttribType.Uint8, true, false);
                bgfx.vertex_layout_end(g_VertexLayout_address);
            }


            g_AttribLocationTex = bgfx.create_uniform("g_AttribLocationTex", bgfx.UniformType.Sampler, 1);

            CreateFontsTexture();

            return true;
        }

        public static void InvalidateDeviceObjects()
        {
            bgfx.destroy_uniform(g_AttribLocationTex);
            bgfx.destroy_program(g_ShaderHandle);

            if (!g_FontTexture.Valid)
            {
                bgfx.destroy_texture(g_FontTexture);
                ImGui.GetIO().Fonts.TexID = IntPtr.Zero;
                g_FontTexture.idx = UInt16.MaxValue;
            }
        }

        public static void Init(uint view)
        {
            g_View = view;
            g_FontTexture.idx = UInt16.MaxValue;
            g_ShaderHandle.idx = UInt16.MaxValue;
            g_AttribLocationTex.idx = UInt16.MaxValue;
        }

        public static void Shutdown()
        {
            InvalidateDeviceObjects();
        }

        public static void NewFrame()
        {
            if (!g_FontTexture.Valid)
            {
                CreateDeviceObjects();
            }

            // TODO: please get actual size using your window lib like GLFW and replace below code.
            ImGuiIOPtr io = ImGui.GetIO();

            int w = 720, h = 480;
            int display_w = 720, display_h = 480;

            io.DisplaySize = new System.Numerics.Vector2((float)w, (float)h);
            if (w > 0 && h > 0)
                io.DisplayFramebufferScale = new System.Numerics.Vector2((float)display_w / (float)w, (float)display_h / (float)h);

            io.DeltaTime = (float)(1.0f / 60.0f);
        }
    }
}
