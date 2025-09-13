using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace storytor.Game.Components
{
    /// <summary>
    /// Grid overlay showing osu! coordinate system (640x480) with mouse coordinates
    /// </summary>
    public partial class GridOverlay : Container
    {
        private Container gridContainer;
        private SpriteText coordinateLabel;

        public GridOverlay()
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Children = new Drawable[]
            {
                // Full screen grid
                createFullScreenGrid(),
                // Grid container that matches actual storyboard aspect ratio
                gridContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                // Coordinate display with orange badge
                new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Margin = new MarginPadding { Top = 20, Right = 20 },
                    Masking = true,
                    CornerRadius = 8,
                    Children = new Drawable[]
                    {
                        // Orange rounded background
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(1f, 0.647f, 0f, 1.0f) // Orange with transparency
                        },
                        // Text with padding on top of background
                        coordinateLabel = new SpriteText
                        {
                            Text = "X: 0, Y: 0",
                            Font = FontUsage.Default.With(size: 12),
                            Colour = Color4.White,
                            Margin = new MarginPadding { Horizontal = 8, Vertical = 4 }
                        }
                    }
                }
            };

            Console.WriteLine($"🔧 GridOverlay LoadComplete - coordinateLabel created: {coordinateLabel != null}");
        }

        private Container createFullScreenGrid()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = new GridContainer()
            };
        }

        private partial class GridContainer : Container
        {
            private readonly int gridSpacing = 20;
            private readonly Color4 gridColor = new Color4(1, 1, 1, 0.15f);
            private readonly Color4 centerLineColor = new Color4(1, 1, 1, 0.25f);

            public GridContainer()
            {
                RelativeSizeAxes = Axes.Both;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Console.WriteLine($"🔧 GridContainer LoadComplete called");
                rebuildGrid();
            }

            private void rebuildGrid()
            {
                Clear();
                Console.WriteLine($"🔧 Rebuilding grid - clearing previous children");

                // Wait for the container to have proper size
                Schedule(() =>
                {
                    var screenWidth = Parent?.DrawWidth ?? DrawWidth;
                    var screenHeight = Parent?.DrawHeight ?? DrawHeight;

                    // If still no size, get from root container
                    if (screenWidth <= 0 || screenHeight <= 0)
                    {
                        var rootContainer = (CompositeDrawable)this;
                        while (rootContainer.Parent != null) rootContainer = rootContainer.Parent;
                        screenWidth = rootContainer.DrawWidth;
                        screenHeight = rootContainer.DrawHeight;
                    }

                    Console.WriteLine($"🔧 Grid dimensions: {screenWidth}x{screenHeight}");

                    if (screenWidth <= 0 || screenHeight <= 0)
                    {
                        Console.WriteLine($"🔧 Invalid dimensions, retrying in Update");
                        return;
                    }

                    // Calculate the center position where osu! coordinate (320, 240) should be
                    var centerX = screenWidth / 2f;
                    var centerY = screenHeight / 2f;

                    // Calculate how many grid lines we need
                    var linesLeft = (int)Math.Ceiling(centerX / gridSpacing) + 2;
                    var linesRight = (int)Math.Ceiling((screenWidth - centerX) / gridSpacing) + 2;
                    var linesUp = (int)Math.Ceiling(centerY / gridSpacing) + 2;
                    var linesDown = (int)Math.Ceiling((screenHeight - centerY) / gridSpacing) + 2;

                    int lineCount = 0;

                    // Vertical lines
                    for (int i = -linesLeft; i <= linesRight; i++)
                    {
                        var x = centerX + (i * gridSpacing);
                        if (x >= -10 && x <= screenWidth + 10)
                        {
                            var isCenter = (i == 0);
                            Add(new Box
                            {
                                Width = isCenter ? 2 : 1,
                                RelativeSizeAxes = Axes.Y,
                                Colour = isCenter ? centerLineColor : gridColor,
                                Position = new Vector2(x, 0)
                            });
                            lineCount++;
                        }
                    }

                    // Horizontal lines
                    for (int i = -linesUp; i <= linesDown; i++)
                    {
                        var y = centerY + (i * gridSpacing);
                        if (y >= -10 && y <= screenHeight + 10)
                        {
                            var isCenter = (i == 0);
                            Add(new Box
                            {
                                Height = isCenter ? 2 : 1,
                                RelativeSizeAxes = Axes.X,
                                Colour = isCenter ? centerLineColor : gridColor,
                                Position = new Vector2(0, y)
                            });
                            lineCount++;
                        }
                    }

                    Console.WriteLine($"🔧 Added {lineCount} grid lines");
                });
            }

            private float lastWidth = 0;
            private float lastHeight = 0;

            protected override void Update()
            {
                base.Update();

                // Rebuild grid if size changed significantly or no children
                if (Parent != null)
                {
                    var currentWidth = Parent.DrawWidth;
                    var currentHeight = Parent.DrawHeight;

                    if (Children.Count == 0 ||
                        Math.Abs(currentWidth - lastWidth) > 10 ||
                        Math.Abs(currentHeight - lastHeight) > 10)
                    {
                        Console.WriteLine($"🔧 GridContainer Update: size changed {currentWidth}x{currentHeight}, rebuilding");
                        rebuildGrid();
                        lastWidth = currentWidth;
                        lastHeight = currentHeight;
                    }
                }
            }
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            var localPos = ToLocalSpace(e.ScreenSpaceMousePosition);

            // Calculate the actual storyboard bounds within the window
            var windowWidth = DrawWidth;
            var windowHeight = DrawHeight;
            var storyboardAspectRatio = 854f / 480f; // osu! widescreen aspect ratio
            var windowAspectRatio = windowWidth / windowHeight;

            float storyboardWidth, storyboardHeight;
            float offsetX = 0, offsetY = 0;

            if (windowAspectRatio > storyboardAspectRatio)
            {
                // Window is wider than storyboard - letterboxing on sides
                storyboardHeight = windowHeight;
                storyboardWidth = storyboardHeight * storyboardAspectRatio;
                offsetX = (windowWidth - storyboardWidth) / 2f;
            }
            else
            {
                // Window is taller than storyboard - letterboxing on top/bottom
                storyboardWidth = windowWidth;
                storyboardHeight = storyboardWidth / storyboardAspectRatio;
                offsetY = (windowHeight - storyboardHeight) / 2f;
            }

            // Convert to storyboard-relative coordinates (0-854, 0-480) - allow values outside bounds
            var storyboardX = (localPos.X - offsetX) / storyboardWidth * 854f;
            var storyboardY = (localPos.Y - offsetY) / storyboardHeight * 480f;

            // Convert to osu! coordinates (-107 to 747 for X, 0 to 480 for Y) - no clamping
            var osuX = (int)(storyboardX - 107);
            var osuY = (int)storyboardY;

            coordinateLabel.Text = $"X: {osuX}, Y: {osuY}";

            return false; // Don't consume the event
        }

        public new void Show()
        {
            this.FadeIn(200);
        }

        public new void Hide()
        {
            this.FadeOut(200);
        }
    }
}
