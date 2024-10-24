using OpenTK.Windowing.Desktop;

namespace Volumetric {
    internal class Program {
        static void Main() {
            GameWindowSettings settings = GameWindowSettings.Default;
            settings.UpdateFrequency = 144;

            using Game game = new(800, 600, "Volumetric", settings);
            game.Run();
        }
    }
}