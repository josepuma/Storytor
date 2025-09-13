using System;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace storytor.Game.Components
{
    /// <summary>
    /// Interactive timeline component for audio navigation
    /// </summary>
    public partial class TimelineComponent : Container
    {
        private Track currentTrack;
        private Container progressContainer;
        private Box progressBar;
        private SpriteText timeLabel;
        private SpriteText durationLabel;
        private bool isDragging = false;

        public event Action<double> OnSeek;

        public TimelineComponent()
        {
            RelativeSizeAxes = Axes.X;
            Height = 40;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Horizontal = 20, Vertical = 20 },
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        // Progress bar container that defines the actual bar area
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 6,
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                // Current time label - aligned with start of this container
                                timeLabel = new SpriteText
                                {
                                    Text = "0:00",
                                    Colour = Color4.White,
                                    Font = FontUsage.Default.With(size: 12),
                                    Anchor = Anchor.TopLeft,
                                    Origin = Anchor.BottomCentre,
                                    X = 10,
                                    Y = -10
                                },
                                // Duration label - aligned with end of this container
                                durationLabel = new SpriteText
                                {
                                    Text = "0:00",
                                    Colour = Color4.White,
                                    Font = FontUsage.Default.With(size: 12),
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.BottomCentre,
                                    X = -10,
                                    Y = -10
                                },
                                // Background track (grey)
                                new CircularContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Masking = true,
                                    Child = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(1, 1, 1, 0.2f)
                                    }
                                },
                                // Progress container (clips the orange fill)
                                progressContainer = new CircularContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Masking = true,
                                    Child = progressBar = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(1f, 0.647f, 0f, 1f), // Orange fill
                                        Width = 0f
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public void SetTrack(Track track)
        {
            currentTrack = track;
            if (track != null)
            {
                durationLabel.Text = formatTime(track.Length);
            }
            else
            {
                durationLabel.Text = "0:00";
            }
        }

        public void UpdateProgress(double currentTime)
        {
            if (currentTrack == null || isDragging) return;

            // Update time label
            timeLabel.Text = formatTime(currentTime);

            // Update duration label if it changed
            if (currentTrack.Length > 0 && durationLabel.Text == "0:00")
            {
                durationLabel.Text = formatTime(currentTrack.Length);
            }

            // Update progress bar width
            if (currentTrack.Length > 0)
            {
                float progress = (float)(currentTime / currentTrack.Length);
                progressBar.Width = Math.Clamp(progress, 0f, 1f);
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (currentTrack == null) return false;

            isDragging = true;
            seekToPosition(e.MousePosition.X);
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            isDragging = false;
            base.OnMouseUp(e);
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            if (!isDragging || currentTrack == null) return false;

            seekToPosition(e.MousePosition.X);
            return true;
        }

        private void seekToPosition(float mouseX)
        {
            // Calculate position within the progress bar area
            var progressArea = progressContainer.Parent;
            var localPos = progressArea.ToLocalSpace(ToScreenSpace(new Vector2(mouseX, 0)));

            // Calculate seek percentage
            float percentage = Math.Clamp(localPos.X / progressArea.DrawWidth, 0f, 1f);
            double seekTime = percentage * currentTrack.Length;

            // Update visual immediately for responsiveness
            timeLabel.Text = formatTime(seekTime);
            progressBar.Width = percentage;

            // Notify parent to seek
            OnSeek?.Invoke(seekTime);
        }

        private string formatTime(double milliseconds)
        {
            var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:D2}";
        }
    }
}
