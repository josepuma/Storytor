using System;
using System.Collections.Generic;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osuTK;
using storytor.Game.Storyboard.Models;
using storytor.Game.Storyboard.Rendering;
using storytor.Game.Services;
using storytor.Game.Beatmap.Models;

namespace storytor.Game.Screens
{
    /// <summary>
    /// Simplified StoryboardRenderer using timeline-based rendering
    /// </summary>
    public partial class StoryboardRenderer : Container
    {
        private readonly StoryboardData storyboard;
        private readonly string basePath;
        private readonly BeatmapData beatmap;
        private readonly Dictionary<StoryboardSprite, SpriteTimelineManager> timelineManagers = new();
        private readonly Dictionary<StoryboardSprite, AnimatedStoryboardSprite> spriteDrawables = new();
        [Resolved]
        private GameHost host { get; set; } = null!;

        private Container storyboardContainer;
        private const float widescreenwidth = 854f;
        private const float standardwidth = 640f;
        private const float storyboardheight = 480f;
        private const float xoffset = (widescreenwidth - standardwidth) / 2f;

        public StoryboardRenderer(StoryboardData storyboard, string basePath, BeatmapData beatmap = null)
        {
            this.storyboard = storyboard;
            this.basePath = basePath;
            this.beatmap = beatmap;

            RelativeSizeAxes = Axes.Both;

            createStoryboardContainers();
        }

        private void createStoryboardContainers()
        {
            // osu! always uses 854x480 coordinate system internally, regardless of widescreen setting
            // The widescreen setting affects how content is displayed, not the coordinate system
            Console.WriteLine($"🎭 Creating storyboard container: {widescreenwidth}x{storyboardheight} (standard osu! coordinates)");
            float containerWidth = (beatmap?.WidescreenStoryboard ?? true) ? widescreenwidth : standardwidth;
            // Create storyboard coordinate container using standard osu! dimensions
            storyboardContainer = new Container
            {
                Size = new Vector2(containerWidth, storyboardheight),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Name = "StoryboardContainer",
                Masking = true
            };

            Add(storyboardContainer);

            Console.WriteLine($"✅ Storyboard container created - {containerWidth}x{storyboardheight} with masking (widescreen: {beatmap?.WidescreenStoryboard ?? true})");
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Initialize timeline managers and create drawable sprites for all sprites
            foreach (var sprite in storyboard.Sprites)
            {
                timelineManagers[sprite] = new SpriteTimelineManager(sprite);

                // Create drawable sprite
                var drawable = createSpriteDrawable(sprite);
                if (drawable != null)
                {
                    spriteDrawables[sprite] = drawable;
                    storyboardContainer.Add(drawable);
                }
            }
        }

        private AnimatedStoryboardSprite createSpriteDrawable(StoryboardSprite sprite)
        {
            // Clean and normalize the image path
            var cleanImagePath = sprite.ImagePath?.Trim().Trim('"') ?? "";

            if (string.IsNullOrEmpty(cleanImagePath))
            {
                return null;
            }

            // Try different path combinations like the original
            var possiblePaths = new[]
            {
                Path.Combine(basePath, cleanImagePath.Replace('\\', '/')),
                Path.Combine(basePath, cleanImagePath.Replace('/', '\\')),
                Path.Combine(basePath, Path.GetFileName(cleanImagePath)),
                Path.Combine(basePath, "sb", cleanImagePath.Replace('\\', '/')),
                Path.Combine(basePath, "SB", cleanImagePath.Replace('\\', '/')),
                cleanImagePath // Try absolute path
            };

            string foundPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    foundPath = path;
                    break;
                }
            }

            if (foundPath == null)
            {
                return null;
            }

            try
            {
                // Use texture cache to avoid loading the same texture multiple times
                var texture = getOrLoadTexture(foundPath);

                // Calculate offset based on widescreen setting
                float currentOffset = (beatmap?.WidescreenStoryboard ?? true) ? xoffset : 0;
                var adjustedX = sprite.X + currentOffset;

                var drawable = new AnimatedStoryboardSprite(sprite, texture)
                {
                    Position = new Vector2(adjustedX, sprite.Y),
                    Origin = sprite.Origin,
                    Alpha = 0 // Start invisible, will be controlled by timeline
                };

                return drawable;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Texture getOrLoadTexture(string imagePath)
        {
            // Use shared texture cache service
            return TextureCacheService.Instance.GetOrLoadTexture(imagePath);
        }

        private Anchor convertOriginToAnchor(Anchor origin)
        {
            // Direct mapping since osu.Framework uses the same enum values
            return origin;
        }

        protected override void Update()
        {
            base.Update();

            // Update container scale to maintain aspect ratio while fitting screen
            updateContainerScale();
        }

        private void updateContainerScale()
        {
            if (storyboardContainer == null) return;

            // Calculate scale to fit the screen while maintaining 854x480 aspect ratio (16:9)
            var screenSize = DrawSize;
            var targetAspectRatio = 854f / 480f;
            var screenAspectRatio = screenSize.X / screenSize.Y;

            float scale;
            if (screenAspectRatio > targetAspectRatio)
            {
                // Screen is wider - scale to fit height
                scale = screenSize.Y / 480f;
            }
            else
            {
                // Screen is taller - scale to fit width
                scale = screenSize.X / 854f;
            }

            storyboardContainer.Scale = new Vector2(scale);
        }

        private void updateSpriteAtTime(AnimatedStoryboardSprite drawable, StoryboardSprite sprite, SpriteTimelineManager timelineManager, double time)
        {
            // Check if sprite should be visible
            if (!timelineManager.IsVisibleAt(time))
            {
                drawable.Alpha = 0f;
                return;
            }

            // Get sprite state at current time with dynamic offset
            float currentOffset = (beatmap?.WidescreenStoryboard ?? true) ? xoffset : 0;
            var state = timelineManager.GetStateAt(time, currentOffset);

            // Apply state to drawable
            drawable.Position = state.Position;
            drawable.Scale = state.Scale;
            drawable.Rotation = state.Rotation;
            drawable.Alpha = state.Alpha;
            drawable.Colour = state.Color;

            // Apply flip transformations
            if (state.FlipH)
                drawable.Scale = new Vector2(-Math.Abs(drawable.Scale.X), drawable.Scale.Y);
            if (state.FlipV)
                drawable.Scale = new Vector2(drawable.Scale.X, -Math.Abs(drawable.Scale.Y));

            // Apply blending mode (simplified)
            if (state.Additive)
                drawable.Blending = BlendingParameters.Additive;
            else
                drawable.Blending = BlendingParameters.Mixture;
        }


        /// <summary>
        /// Updates the storyboard to a specific time (main method called from outside)
        /// </summary>
        public void UpdateTime(double timeMs)
        {
            // Update all sprites based on given time
            foreach (var (sprite, timelineManager) in timelineManagers)
            {
                if (!spriteDrawables.TryGetValue(sprite, out var drawable)) continue;

                updateSpriteAtTime(drawable, sprite, timelineManager, timeMs);
            }
        }

        /// <summary>
        /// Seeks the storyboard to a specific time (alias for UpdateTime)
        /// </summary>
        public void SeekTo(double time)
        {
            UpdateTime(time);
        }

        /// <summary>
        /// Gets the time range where the storyboard has content
        /// </summary>
        public (double startTime, double endTime) GetContentTimeRange()
        {
            double startTime = double.MaxValue;
            double endTime = double.MinValue;

            foreach (var timelineManager in timelineManagers.Values)
            {
                startTime = Math.Min(startTime, timelineManager.DisplayStartTime);
                endTime = Math.Max(endTime, timelineManager.DisplayEndTime);
            }

            return startTime == double.MaxValue ? (0, 0) : (startTime, endTime);
        }

        /// <summary>
        /// Gets debug information about the storyboard state
        /// </summary>
        public string GetDebugInfo(double time)
        {
            var activeSprites = 0;
            var visibleSprites = 0;

            foreach (var (sprite, timelineManager) in timelineManagers)
            {
                if (timelineManager.IsActiveAt(time)) activeSprites++;
                if (timelineManager.IsVisibleAt(time)) visibleSprites++;
            }

            var (startTime, endTime) = GetContentTimeRange();

            return $"Time: {time:F0}ms | Content: {startTime:F0}-{endTime:F0}ms | " +
                    $"Active: {activeSprites}/{timelineManagers.Count} | Visible: {visibleSprites}";
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // Clear caches (texture cache is now managed globally)
            timelineManagers.Clear();
            spriteDrawables.Clear();
        }
    }

    /// <summary>
    /// A drawable sprite that can be animated based on storyboard commands
    /// </summary>
    public partial class AnimatedStoryboardSprite : Sprite
    {
        public StoryboardSprite StoryboardData { get; }

        public AnimatedStoryboardSprite(StoryboardSprite storyboardData, Texture texture)
        {
            StoryboardData = storyboardData;
            Texture = texture;

            // Set initial properties
            Name = $"Sprite: {storyboardData.DisplayName}";
        }

        public override string ToString()
        {
            return $"AnimatedSprite: {StoryboardData.DisplayName} at ({Position.X}, {Position.Y})";
        }
    }
}
