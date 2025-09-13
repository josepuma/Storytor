using osu.Framework.iOS;
using storytor.Game;

namespace storytor.iOS
{
    /// <inheritdoc />
    public class AppDelegate : GameApplicationDelegate
    {
        /// <inheritdoc />
        protected override osu.Framework.Game CreateGame() => new storytorGame();
    }
}
