using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

public static class Program
{
    static void Main()
    {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(800, 600),
                Title = "DMG",
                // This is needed to run on macos
                Flags = ContextFlags.ForwardCompatible,
            };

            using (var window = new OglRenderer.Window(GameWindowSettings.Default, nativeWindowSettings))
            {

                window.Run();
            
            /*
            while (true)
                {
                    window.ProcessEvents(0d);
                    window.GameLoop();
                }
            */
            }
    }
}
