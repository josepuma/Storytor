using osu.Framework.Platform;
using osu.Framework;
using storytor.Game;

namespace storytor.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"storytor"))
            using (osu.Framework.Game game = new storytorGame())
                host.Run(game);
        }
    }
}
