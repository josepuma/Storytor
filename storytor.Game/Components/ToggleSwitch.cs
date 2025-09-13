using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace storytor.Game.Components
{
    /// <summary>
    /// A modern toggle switch component
    /// </summary>
    public partial class ToggleSwitch : BasicButton
    {
        private Box background;
        private CircularContainer knob;
        private bool isOn = false;

        public event Action<bool> OnToggle;

        public bool Value
        {
            get => isOn;
            set
            {
                if (isOn == value) return;
                isOn = value;
                updateAppearance();
                OnToggle?.Invoke(isOn);
            }
        }

        public ToggleSwitch()
        {
            Size = new Vector2(40, 20);
            CornerRadius = 10;
            Masking = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                // Background
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(0.3f, 0.3f, 0.3f, 1f)
                },
                // Knob
                knob = new CircularContainer
                {
                    Size = new Vector2(16, 16),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.Centre,
                    X = 10,
                    Masking = true,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White
                    }
                }
            };

            updateAppearance();
        }

        protected override bool OnClick(ClickEvent e)
        {
            Value = !Value;
            return true;
        }

        private void updateAppearance()
        {
            if (background == null) return;

            // Animate colors and position
            background.FadeColour(isOn ? new Color4(0f, 0.7f, 1f, 1f) : new Color4(0.3f, 0.3f, 0.3f, 1f), 200);
            knob?.MoveTo(new Vector2(isOn ? 30 : 10, 0), 200, Easing.OutQuint);
        }
    }
}