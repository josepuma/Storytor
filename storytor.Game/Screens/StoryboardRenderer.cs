using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osuTK;
using storytor.Game.Storyboard.Models;
using storytor.Game.Storyboard.Rendering;

namespace storytor.Game.Screens
{
    /// <summary>
    /// Simplified StoryboardRenderer using timeline-based rendering
    /// </summary>
    public partial class StoryboardRenderer : Container
    {
        private readonly StoryboardData storyboard;
        private readonly string basePath;
        private readonly Dictionary<StoryboardSprite, SpriteTimelineManager> timelineManagers = new();
        private readonly Dictionary<StoryboardSprite, AnimatedStoryboardSprite> spriteDrawables = new();
        private readonly Dictionary<string, Texture> textureCache = new();
        private const int MAX_TEXTURE_CACHE_SIZE = 1000; // Limit memory usage

        // Sprite pooling for performance
        private readonly Dictionary<StoryboardSprite, bool> spriteVisibilityCache = new();
        private int activeSpritesCount = 0;
        
        [Resolved]
        private GameHost host { get; set; } = null!;
        
        private Container storyboardContainer;
        private const float STORYBOARD_WIDTH = 854f;
        private const float STORYBOARD_HEIGHT = 480f;
        private const float X_OFFSET = (STORYBOARD_WIDTH - 640f) / 2f;

        public StoryboardRenderer(StoryboardData storyboard, string basePath)
        {
            this.storyboard = storyboard;
            this.basePath = basePath;
            
            RelativeSizeAxes = Axes.Both;
            
            // Create storyboard coordinate container
            storyboardContainer = new Container
            {
                Size = new Vector2(STORYBOARD_WIDTH, STORYBOARD_HEIGHT),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Name = "StoryboardContainer"
            };
            
            Add(storyboardContainer);
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
                    // Don't add to container yet - will be added on-demand via SetSpriteVisibility
                    spriteVisibilityCache[sprite] = false;
                }
            }
        }

        private AnimatedStoryboardSprite createSpriteDrawable(StoryboardSprite sprite)
        {
            try
            {
                // Create animated sprite with adjusted coordinates for 16:9 container
                var adjustedX = sprite.X + X_OFFSET;

                AnimatedStoryboardSprite drawable;

                if (sprite.IsAnimation)
                {
                    // Load multiple textures for animation frames
                    var textures = loadAnimationTextures(sprite);
                    if (textures == null || textures.Count == 0)
                        return null;

                    drawable = new AnimatedStoryboardSprite(sprite, textures)
                    {
                        Position = new Vector2(adjustedX, sprite.Y),
                        Origin = sprite.Origin,
                        Alpha = 0 // Start invisible, will be controlled by timeline
                    };
                }
                else
                {
                    // Load single texture for regular sprite
                    var texture = loadSingleTexture(sprite.ImagePath);
                    if (texture == null)
                        return null;

                    drawable = new AnimatedStoryboardSprite(sprite, texture)
                    {
                        Position = new Vector2(adjustedX, sprite.Y),
                        Origin = sprite.Origin,
                        Alpha = 0 // Start invisible, will be controlled by timeline
                    };
                }

                return drawable;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Texture loadSingleTexture(string imagePath)
        {
            // Clean and normalize the image path
            var cleanImagePath = imagePath?.Trim().Trim('"') ?? "";

            if (string.IsNullOrEmpty(cleanImagePath))
                return null;

            string foundPath = findImagePath(cleanImagePath);
            if (foundPath == null)
                return null;

            return getOrLoadTexture(foundPath);
        }

        private List<Texture> loadAnimationTextures(StoryboardSprite sprite)
        {
            var textures = new List<Texture>();
            var cleanImagePath = sprite.ImagePath?.Trim().Trim('"') ?? "";

            if (string.IsNullOrEmpty(cleanImagePath))
                return null;

            // Remove extension to get base name
            var baseName = Path.GetFileNameWithoutExtension(cleanImagePath);
            var extension = Path.GetExtension(cleanImagePath);
            var directory = Path.GetDirectoryName(cleanImagePath) ?? "";

            // Load frames: baseName0.ext, baseName1.ext, etc.
            for (int i = 0; i < sprite.FrameCount; i++)
            {
                var frameName = $"{baseName}{i}{extension}";
                var framePath = Path.Combine(directory, frameName);

                string foundPath = findImagePath(framePath);
                if (foundPath == null)
                {
                    Console.WriteLine($"Animation frame not found: {framePath}");
                    return null; // If any frame is missing, fail the animation
                }

                var texture = getOrLoadTexture(foundPath);
                if (texture == null)
                {
                    Console.WriteLine($"Failed to load animation frame: {foundPath}");
                    return null;
                }

                textures.Add(texture);
            }

            return textures;
        }

        private string findImagePath(string imagePath)
        {
            // Try different path combinations
            var possiblePaths = new[]
            {
                Path.Combine(basePath, imagePath.Replace('\\', '/')),
                Path.Combine(basePath, imagePath.Replace('/', '\\')),
                Path.Combine(basePath, Path.GetFileName(imagePath)),
                Path.Combine(basePath, "sb", imagePath.Replace('\\', '/')),
                Path.Combine(basePath, "SB", imagePath.Replace('\\', '/')),
                imagePath // Try absolute path
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// Efficiently manages sprite visibility and pooling
        /// </summary>
        private void SetSpriteVisibility(StoryboardSprite sprite, AnimatedStoryboardSprite drawable, bool visible)
        {
            // Check if visibility state has changed
            bool wasVisible = spriteVisibilityCache.GetValueOrDefault(sprite, false);

            if (wasVisible == visible)
                return; // No change needed

            spriteVisibilityCache[sprite] = visible;

            if (visible)
            {
                // Add sprite to render tree if not already there
                if (!storyboardContainer.Contains(drawable))
                {
                    storyboardContainer.Add(drawable);
                }
                activeSpritesCount++;
            }
            else
            {
                // Remove sprite from render tree to save rendering cost
                if (storyboardContainer.Contains(drawable))
                {
                    storyboardContainer.Remove(drawable, disposeImmediately: false);
                }
                drawable.Alpha = 0f; // Ensure it's invisible
                activeSpritesCount = Math.Max(0, activeSpritesCount - 1);
            }
        }

        private Texture getOrLoadTexture(string imagePath)
        {
            // Use the full path as cache key
            if (textureCache.TryGetValue(imagePath, out var cachedTexture))
                return cachedTexture;

            // Limit cache size to prevent memory issues
            if (textureCache.Count >= MAX_TEXTURE_CACHE_SIZE)
            {
                // Remove oldest entries (simple FIFO eviction)
                var keysToRemove = textureCache.Keys.Take(100).ToList();
                foreach (var key in keysToRemove)
                {
                    textureCache[key]?.Dispose();
                    textureCache.Remove(key);
                }
            }

            try
            {
                using var stream = File.OpenRead(imagePath);
                var texture = Texture.FromStream(host.Renderer, stream);
                textureCache[imagePath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture {imagePath}: {ex.Message}");
                return null;
            }
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
            // Update animation frame if it's an animated sprite
            drawable.UpdateAnimation(time);

            // Check if sprite should be visible
            if (!timelineManager.IsVisibleAt(time))
            {
                drawable.Alpha = 0f;
                return;
            }

            // Get sprite state at current time
            var state = timelineManager.GetStateAt(time, X_OFFSET);

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
            // Get screen bounds for culling
            var screenSize = DrawSize;
            var cullMargin = 200f; // Extra margin for sprites partially off-screen

            // Update only relevant sprites
            foreach (var (sprite, timelineManager) in timelineManagers)
            {
                if (!spriteDrawables.TryGetValue(sprite, out var drawable)) continue;

                // Skip sprites that are completely outside their active time range
                if (!timelineManager.IsActiveAt(timeMs))
                {
                    SetSpriteVisibility(sprite, drawable, false);
                    continue;
                }

                // Basic visibility check first (cheaper than full update)
                if (!timelineManager.IsVisibleAt(timeMs))
                {
                    SetSpriteVisibility(sprite, drawable, false);
                    continue;
                }

                // Screen culling - skip sprites outside screen bounds
                var spritePos = new Vector2(sprite.X + X_OFFSET, sprite.Y);
                if (spritePos.X < -cullMargin ||
                    spritePos.X > STORYBOARD_WIDTH + cullMargin ||
                    spritePos.Y < -cullMargin ||
                    spritePos.Y > STORYBOARD_HEIGHT + cullMargin)
                {
                    // Check if sprite has active movement that might bring it on-screen
                    var moveTimeline = timelineManager.GetTimeline("M");
                    var moveXTimeline = timelineManager.GetTimeline("MX");
                    var moveYTimeline = timelineManager.GetTimeline("MY");

                    bool hasActiveMovement = (moveTimeline?.GetActiveCommand(timeMs) != null) ||
                                           (moveXTimeline?.GetActiveCommand(timeMs) != null) ||
                                           (moveYTimeline?.GetActiveCommand(timeMs) != null);

                    if (!hasActiveMovement)
                    {
                        SetSpriteVisibility(sprite, drawable, false);
                        continue;
                    }
                }

                // Perform full update for visible, on-screen sprites
                SetSpriteVisibility(sprite, drawable, true);
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
        /// Gets debug information about the storyboard state (optimized)
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
                   $"Active: {activeSprites}/{timelineManagers.Count} | Visible: {visibleSprites} | " +
                   $"Rendered: {activeSpritesCount} | Textures: {textureCache.Count}";
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        public (int totalSprites, int activeSprites, int renderedSprites, int cachedTextures) GetPerformanceStats()
        {
            return (timelineManagers.Count, spriteVisibilityCache.Count(kvp => kvp.Value), activeSpritesCount, textureCache.Count);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            
            // Clear caches
            textureCache.Clear();
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

        // Animation properties
        private readonly List<Texture> animationFrames;
        private readonly bool isAnimation;
        private int currentFrame = 0;
        private double lastFrameTime = 0;

        // Constructor for regular sprites
        public AnimatedStoryboardSprite(StoryboardSprite storyboardData, Texture texture)
        {
            StoryboardData = storyboardData;
            Texture = texture;
            isAnimation = false;

            // Set initial properties
            Name = $"Sprite: {storyboardData.DisplayName}";
        }

        // Constructor for animated sprites
        public AnimatedStoryboardSprite(StoryboardSprite storyboardData, List<Texture> textures)
        {
            StoryboardData = storyboardData;
            animationFrames = textures;
            isAnimation = true;

            if (textures?.Count > 0)
            {
                Texture = textures[0]; // Start with first frame
                currentFrame = 0;
            }

            // Set initial properties
            Name = $"Animation: {storyboardData.DisplayName}";
        }

        /// <summary>
        /// Updates the animation frame based on current time
        /// </summary>
        public void UpdateAnimation(double currentTime)
        {
            if (!isAnimation || animationFrames == null || animationFrames.Count <= 1)
                return;

            // Initialize timing if this is the first update
            if (lastFrameTime == 0)
                lastFrameTime = currentTime;

            // Check if it's time to advance to next frame
            double timeSinceLastFrame = currentTime - lastFrameTime;
            if (timeSinceLastFrame >= StoryboardData.FrameDelay)
            {
                // Advance frame
                if (StoryboardData.LoopType == "LoopForever" ||
                    (StoryboardData.LoopType == "LoopOnce" && currentFrame < animationFrames.Count - 1))
                {
                    currentFrame = (currentFrame + 1) % animationFrames.Count;
                    Texture = animationFrames[currentFrame];
                    lastFrameTime = currentTime;
                }
                // For LoopOnce, stay on last frame after animation completes
            }
        }

        public override string ToString()
        {
            if (isAnimation)
                return $"Animation: {StoryboardData.DisplayName} Frame {currentFrame}/{animationFrames?.Count ?? 0} at ({Position.X}, {Position.Y})";
            else
                return $"Sprite: {StoryboardData.DisplayName} at ({Position.X}, {Position.Y})";
        }
    }
}