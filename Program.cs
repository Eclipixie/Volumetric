namespace Volumetric {
    internal class Program {
        static void Main() {
            // See https://aka.ms/new-console-template for more information

            using Game game = new(800, 600, "Volumetric");
            game.Run();
        }
    }
}