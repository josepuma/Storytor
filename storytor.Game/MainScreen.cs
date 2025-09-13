using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using storytor.Game.Screens;
using osuTK.Graphics;

namespace storytor.Game
{
    public partial class MainScreen : Screen
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.DarkSlateGray,
                    RelativeSizeAxes = Axes.Both,
                },
                new SpriteText
                {
                    Y = 40,
                    Text = "Storytor - OSU! Storyboard Player",
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Font = FontUsage.Default.With(size: 32),
                    Colour = Colour4.White
                },
                new SpriteText
                {
                    Y = 80,
                    Text = "Load and play OSU! storyboards with synchronized audio",
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Font = FontUsage.Default.With(size: 16),
                    Colour = Colour4.LightGray
                },
                new BasicButton
                {
                    Text = "Open Storyboard Player",
                    Size = new osuTK.Vector2(300, 60),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    BackgroundColour = Colour4.DarkBlue,
                    Action = () => this.Push(new StoryboardScreen())
                }
            };
        }
    }
}
