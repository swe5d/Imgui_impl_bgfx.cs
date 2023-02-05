# Imgui_impl_bgfx.cs
A Dear-Imgui backend implement by Bgfx in C#


# How to use
I will use VS2022 and OpenGL backend as example...

1. first please get Bgfx and its dependencies:
https://bkaradzic.github.io/bgfx/build.html

2. build bgfx as a shared lib solution, refers to https://github.com/bkaradzic/bgfx/issues/297:
```
$ genie --with-shared-lib vs2022
```
3. open your bgfx solution, you will find a new project, please set it as start project and build it.
   
![bgfx-dll-proj](screenshots\bgfx-dll-proj.png "bgfx-dll-proj")

you will find **bgfx-shared-libDebug.dll**, please copy and paste this dll along with your C#'s executable file.
and don't forget to include bgfx's C# bindings to your project.

4. In your C# project, add nuget package: ImGui.NET. I use ImGui.NET 1.89.3 for testing.
5. include this repo's two .cs files to your C# project.
6. In your render loop, write like this:
   ```C#
    using <YOUR_WINDOW_LIB>;
    using Bgfx;
    using ImGuiNET;
    using System;

    namespace CSharpBgfxTest
    {
        class Program
        {
            unsafe static void Main (string[] args)
            {
                // Setup your own window lib context
                initWindowContext();
                // Setup Dear ImGui context
                ImGui.CreateContext();
                ImGui.StyleColorsDark();
                Imgui_impl_bgfx.Init(0);
                
                uint WIDTH = 720, HEIGHT = 480;
                string TITLE = "Simple Window";
                using (var window = new CreateWindow(WIDTH, HEIGHT, TITLE))
                {
                    bgfx.Init bgfxInit;
                    bgfxInit.type = bgfx.RendererType.OpenGL;
                    bgfxInit.platformData.nwh = window.Hwnd;
                    bgfxInit.resolution.width = WIDTH;
                    bgfxInit.resolution.height = HEIGHT;
                    bgfxInit.resolution.reset = (uint)ResetFlags.Vsync;
                    bgfx.init(&bgfxInit);

                    bgfx.set_debug((uint)DebugFlags.Text);
                    bgfx.set_view_clear(
                        0,
                        (ushort)(ClearFlags.Color | ClearFlags.Depth),
                        0x14f11166,
                        1.0f,
                        0
                    );
                    bgfx.set_view_rect(0, 0, 0, WIDTH, HEIGHT);
                    while (!window.IsClosing)
                    {
                        bgfx.touch(0);
                        bgfx.dbg_text_clear(0, false);
                        bgfx.dbg_text_printf(0, 0, 0x4F, "%s", "Hello, world!");
                        bgfx.dbg_text_vprintf(0, 1, 0x4F, "width = %d, height = %d", __arglist  (WIDTH.ToString(), HEIGHT.ToString()));
                        bgfx.frame(false);

                        Imgui_impl_bgfx.NewFrame(); 
                        ImGui.NewFrame();
                        ImGui.Begin("Hello, world!");
                        ImGui.Text("This is some useful text.");
                        ImGui.End();
                        ImGui.Render();
                        Imgui_impl_bgfx.RenderDrawLists(ImGui.GetDrawData());
                    }
                    bgfx.shutdown();
                    Imgui_impl_bgfx.Shutdown();
                }
            }
        }    
    }
   ```


