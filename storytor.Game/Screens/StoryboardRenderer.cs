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
using storytor.Game.Storyboard.Models;
using storytor.Game.Storyboard.Utils;
using storytor.Game.Storyboard.Processing;

namespace storytor.Game.Screens
{
    /// <summary>
    /// Renders a storyboard with proper timing and sprite management
    /// </summary>
    public partial class StoryboardRenderer : Container
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        private readonly StoryboardData storyboard;
        private readonly string basePath;
        private readonly Dictionary<StoryboardSprite, AnimatedStoryboardSprite> spriteDrawables = new Dictionary<StoryboardSprite, AnimatedStoryboardSprite>();
        private readonly Dictionary<StoryboardSprite, List<StoryboardCommand>> expandedCommandsCache = new Dictionary<StoryboardSprite, List<StoryboardCommand>>();
        private readonly Dictionary<StoryboardSprite, Dictionary<Type, List<StoryboardCommand>>> commandsByTypeCache = new Dictionary<StoryboardSprite, Dictionary<Type, List<StoryboardCommand>>>();
        private Container storyboardContainer;

        public StoryboardRenderer(StoryboardData storyboard, string basePath)
        {
            this.storyboard = storyboard;
            this.basePath = basePath;

            // Configure storyboard container to fill the entire screen
            RelativeSizeAxes = Axes.Both;

            // Create a inner container for osu! coordinate system (854x480)
            // that scales to fit the screen height and maintains aspect ratio
            storyboardContainer = new Container
            {
                Size = new osuTK.Vector2(854, 480), // osu! storyboard dimensions (16:9)
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Name = "StoryboardCoordinateContainer"
            };
            Add(storyboardContainer);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Create drawable sprites for each storyboard sprite
            foreach (var storyboardSprite in storyboard.Sprites)
            {
                try
                {
                    var drawable = createDrawableSprite(storyboardSprite);
                    if (drawable != null)
                    {
                        spriteDrawables[storyboardSprite] = drawable;
                        storyboardContainer.Add(drawable);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create drawable for sprite {storyboardSprite.ImagePath}: {ex.Message}");
                }
            }
        }

        private AnimatedStoryboardSprite createDrawableSprite(StoryboardSprite storyboardSprite)
        {
            // Clean and normalize the image path
            var cleanImagePath = storyboardSprite.ImagePath?.Trim().Trim('"') ?? "";

            if (string.IsNullOrEmpty(cleanImagePath))
            {
                return null;
            }

            // Try different path combinations
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
                // Load texture directly from file using framework texture creation
                Texture texture = null;

                // Load texture using osu!framework's Texture.FromStream
                using var stream = File.OpenRead(foundPath);
                texture = Texture.FromStream(host.Renderer, stream);

                // Create animated sprite with adjusted coordinates for 16:9 container
                // osu! coordinates are based on 640x480, but we use 854x480
                // So we need to offset X coordinates to center the 640-wide content in 854-wide container
                var xOffset = (854f - 640f) / 2f; // 107 pixels offset to center
                var adjustedX = storyboardSprite.X + xOffset;

                var drawable = new AnimatedStoryboardSprite(storyboardSprite, texture)
                {
                    Position = new osuTK.Vector2(adjustedX, storyboardSprite.Y),
                    Origin = storyboardSprite.Origin,
                    Alpha = 0 // Start invisible, will be controlled by fade commands
                };

                // Set initial state based on first commands to avoid sudden jumps
                setInitialSpriteState(drawable, storyboardSprite);

                return drawable;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Updates the renderer with the current audio time
        /// </summary>
        /// <param name="timeMs">Current time in milliseconds</param>
        public void UpdateTime(double timeMs)
        {
            // Update all sprite animations

            foreach (var (storyboardSprite, drawable) in spriteDrawables)
            {
                updateSpriteAtTime(drawable, storyboardSprite, timeMs, expandedCommandsCache, commandsByTypeCache);
            }
        }

        private static void updateSpriteAtTime(AnimatedStoryboardSprite drawable, StoryboardSprite storyboardSprite, double timeMs, 
            Dictionary<StoryboardSprite, List<StoryboardCommand>> expandedCommandsCache,
            Dictionary<StoryboardSprite, Dictionary<Type, List<StoryboardCommand>>> commandsByTypeCache)
        {
            // Check if sprite should be visible at this time
            if (!SpriteLifetimeManager.IsSpriteVisible(storyboardSprite, timeMs))
            {
                // Outside sprite lifetime - make invisible
                drawable.Alpha = 0;
                return;
            }

            // Reset sprite to default state before applying commands
            resetSpriteToDefault(drawable, storyboardSprite);

            // Use cached expanded commands or expand and cache them
            if (!expandedCommandsCache.TryGetValue(storyboardSprite, out var allCommands))
            {
                allCommands = new List<StoryboardCommand>();
                foreach (var command in storyboardSprite.Commands)
                {
                    if (command is LoopCommand loopCmd)
                    {
                        allCommands.AddRange(loopCmd.ExpandLoop());
                    }
                    else
                    {
                        allCommands.Add(command);
                    }
                }
                expandedCommandsCache[storyboardSprite] = allCommands;
            }

            // Use cached commands by type or group and cache them
            if (!commandsByTypeCache.TryGetValue(storyboardSprite, out var commandsByType))
            {
                commandsByType = allCommands
                    .GroupBy(c => c.GetType())
                    .ToDictionary(g => g.Key, g => g.OrderBy(c => c.StartTime).ToList());
                commandsByTypeCache[storyboardSprite] = commandsByType;
            }


            foreach (var commandGroup in commandsByType)
            {
                var commandsOfType = commandGroup.Value; // Already sorted from cache

                switch (commandGroup.Key.Name)
                {
                    case nameof(FadeCommand):
                        var fadeCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<FadeCommand>(), timeMs);
                        if (fadeCmd != null)
                        {
                            var opacity = AnimationUtils.InterpolateDouble(timeMs, fadeCmd.StartTime, fadeCmd.EndTime, fadeCmd.StartOpacity, fadeCmd.EndOpacity, fadeCmd.Easing);
                            drawable.Alpha = Math.Clamp((float)opacity, 0f, 1f);
                        }
                        break;

                    case nameof(MoveCommand):
                        var moveCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<MoveCommand>(), timeMs);
                        if (moveCmd != null)
                        {
                            var xOffset = (854f - 640f) / 2f; // Same offset as initial position
                            var startPos = new osuTK.Vector2(moveCmd.StartX + xOffset, moveCmd.StartY);
                            var endPos = new osuTK.Vector2(moveCmd.EndX + xOffset, moveCmd.EndY);
                            var currentPos = AnimationUtils.InterpolateVector2(timeMs, moveCmd.StartTime, moveCmd.EndTime, startPos, endPos, moveCmd.Easing);
                            drawable.Position = currentPos;
                        }
                        break;

                    case nameof(ScaleCommand):
                        var scaleCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<ScaleCommand>(), timeMs);
                        if (scaleCmd != null)
                        {
                            var scale = AnimationUtils.InterpolateDouble(timeMs, scaleCmd.StartTime, scaleCmd.EndTime, scaleCmd.StartScale, scaleCmd.EndScale, scaleCmd.Easing);
                            drawable.Scale = new osuTK.Vector2((float)scale);
                        }
                        break;

                    case nameof(VectorScaleCommand):
                        var vecScaleCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<VectorScaleCommand>(), timeMs);
                        if (vecScaleCmd != null)
                        {
                            var startScale = new osuTK.Vector2(vecScaleCmd.StartScaleX, vecScaleCmd.StartScaleY);
                            var endScale = new osuTK.Vector2(vecScaleCmd.EndScaleX, vecScaleCmd.EndScaleY);
                            var currentScale = AnimationUtils.InterpolateVector2(timeMs, vecScaleCmd.StartTime, vecScaleCmd.EndTime, startScale, endScale, vecScaleCmd.Easing);
                            drawable.Scale = currentScale;
                        }
                        break;

                    case nameof(RotateCommand):
                        var rotateCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<RotateCommand>(), timeMs);
                        if (rotateCmd != null)
                        {
                            var angleRad = AnimationUtils.InterpolateDouble(timeMs, rotateCmd.StartTime, rotateCmd.EndTime, rotateCmd.StartAngle, rotateCmd.EndAngle, rotateCmd.Easing);
                            var angleDegrees = (float)(angleRad * 180.0 / Math.PI);
                            drawable.Rotation = angleDegrees;
                        }
                        break;

                    case nameof(MoveXCommand):
                        var moveXCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<MoveXCommand>(), timeMs);
                        if (moveXCmd != null)
                        {
                            var xOffset = (854f - 640f) / 2f;
                            var currentX = AnimationUtils.InterpolateDouble(timeMs, moveXCmd.StartTime, moveXCmd.EndTime, moveXCmd.StartX + xOffset, moveXCmd.EndX + xOffset, moveXCmd.Easing);
                            drawable.Position = new osuTK.Vector2((float)currentX, drawable.Position.Y);
                        }
                        break;

                    case nameof(MoveYCommand):
                        var moveYCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<MoveYCommand>(), timeMs);
                        if (moveYCmd != null)
                        {
                            var currentY = AnimationUtils.InterpolateDouble(timeMs, moveYCmd.StartTime, moveYCmd.EndTime, moveYCmd.StartY, moveYCmd.EndY, moveYCmd.Easing);
                            drawable.Position = new osuTK.Vector2(drawable.Position.X, (float)currentY);
                        }
                        break;

                    case nameof(ColorCommand):
                        var colorCmd = CommandProcessor.GetActiveCommand(commandsOfType.Cast<ColorCommand>(), timeMs);
                        if (colorCmd != null)
                        {
                            var currentRed = AnimationUtils.InterpolateDouble(timeMs, colorCmd.StartTime, colorCmd.EndTime, colorCmd.StartRed, colorCmd.EndRed, colorCmd.Easing);
                            var currentGreen = AnimationUtils.InterpolateDouble(timeMs, colorCmd.StartTime, colorCmd.EndTime, colorCmd.StartGreen, colorCmd.EndGreen, colorCmd.Easing);
                            var currentBlue = AnimationUtils.InterpolateDouble(timeMs, colorCmd.StartTime, colorCmd.EndTime, colorCmd.StartBlue, colorCmd.EndBlue, colorCmd.Easing);

                            var red = Math.Clamp((float)(currentRed / 255.0), 0f, 1f);
                            var green = Math.Clamp((float)(currentGreen / 255.0), 0f, 1f);
                            var blue = Math.Clamp((float)(currentBlue / 255.0), 0f, 1f);

                            // Preserve the current alpha from fade commands
                            var currentAlpha = drawable.Alpha;
                            drawable.Colour = new Colour4(red, green, blue, 1.0f);
                        }
                        break;

                    case nameof(ParameterCommand):
                        // Apply active parameter commands (parameters only apply while active)
                        var (spriteStart, spriteEnd) = SpriteLifetimeManager.GetSpriteLifetime(storyboardSprite);
                        var activeParameterCmds = CommandProcessor.GetActiveParameterCommands(commandsOfType.Cast<ParameterCommand>(), timeMs, spriteStart, spriteEnd);

                        foreach (var paramCmd in activeParameterCmds)
                        {
                            switch (paramCmd.Parameter)
                            {
                                case "H":
                                    // Flip horizontally by negating X scale
                                    drawable.Scale = new osuTK.Vector2(-Math.Abs(drawable.Scale.X), drawable.Scale.Y);
                                    break;
                                case "V":
                                    // Flip vertically by negating Y scale
                                    drawable.Scale = new osuTK.Vector2(drawable.Scale.X, -Math.Abs(drawable.Scale.Y));
                                    break;
                                case "A":
                                    // Enable additive blending
                                    drawable.Blending = BlendingParameters.Additive;
                                    break;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
        }


        private static void setInitialSpriteState(AnimatedStoryboardSprite drawable, StoryboardSprite storyboardSprite)
        {
            // Both initial state and reset use the same logic to prevent sudden jumps
            applySpriteBaseState(drawable, storyboardSprite);
        }

        private static void resetSpriteToDefault(AnimatedStoryboardSprite drawable, StoryboardSprite storyboardSprite)
        {
            // Reset to sprite's initial state (same as setInitialSpriteState)
            applySpriteBaseState(drawable, storyboardSprite);
        }

        private static void applySpriteBaseState(AnimatedStoryboardSprite drawable, StoryboardSprite storyboardSprite)
        {
            var xOffset = (854f - 640f) / 2f;

            // Find first command of each type to get initial values
            var firstFade = storyboardSprite.Commands.OfType<FadeCommand>().OrderBy(c => c.StartTime).FirstOrDefault();
            var firstMove = storyboardSprite.Commands.OfType<MoveCommand>().OrderBy(c => c.StartTime).FirstOrDefault();
            var firstMoveX = storyboardSprite.Commands.OfType<MoveXCommand>().OrderBy(c => c.StartTime).FirstOrDefault();
            var firstMoveY = storyboardSprite.Commands.OfType<MoveYCommand>().OrderBy(c => c.StartTime).FirstOrDefault();
            var firstScale = storyboardSprite.Commands.OfType<ScaleCommand>().OrderBy(c => c.StartTime).FirstOrDefault();
            var firstVectorScale = storyboardSprite.Commands.OfType<VectorScaleCommand>().OrderBy(c => c.StartTime).FirstOrDefault();
            var firstRotate = storyboardSprite.Commands.OfType<RotateCommand>().OrderBy(c => c.StartTime).FirstOrDefault();
            var firstColor = storyboardSprite.Commands.OfType<ColorCommand>().OrderBy(c => c.StartTime).FirstOrDefault();

            // Set position (prioritize specific move commands over general move command)
            var initialX = storyboardSprite.X + xOffset;
            var initialY = storyboardSprite.Y;

            if (firstMoveX != null)
            {
                initialX = firstMoveX.StartX + xOffset;
            }
            else if (firstMove != null)
            {
                initialX = firstMove.StartX + xOffset;
            }

            if (firstMoveY != null)
            {
                initialY = firstMoveY.StartY;
            }
            else if (firstMove != null)
            {
                initialY = firstMove.StartY;
            }

            drawable.Position = new osuTK.Vector2(initialX, initialY);

            // Set alpha (use first fade command start value if available)
            if (firstFade != null)
            {
                drawable.Alpha = Math.Clamp((float)firstFade.StartOpacity, 0f, 1f);
            }
            else
            {
                drawable.Alpha = 1.0f; // Default visible
            }

            // Set scale (prioritize vector scale over uniform scale)
            if (firstVectorScale != null)
            {
                drawable.Scale = new osuTK.Vector2(firstVectorScale.StartScaleX, firstVectorScale.StartScaleY);
            }
            else if (firstScale != null)
            {
                drawable.Scale = new osuTK.Vector2((float)firstScale.StartScale);
            }
            else
            {
                drawable.Scale = new osuTK.Vector2(1.0f); // Default scale
            }

            // Set rotation
            if (firstRotate != null)
            {
                var angleDegrees = (float)(firstRotate.StartAngle * 180.0 / Math.PI);
                drawable.Rotation = angleDegrees;
            }
            else
            {
                drawable.Rotation = 0.0f; // Default rotation
            }

            // Set color (use first color command start value if available)
            if (firstColor != null)
            {
                var red = Math.Clamp(firstColor.StartRed / 255.0f, 0f, 1f);
                var green = Math.Clamp(firstColor.StartGreen / 255.0f, 0f, 1f);
                var blue = Math.Clamp(firstColor.StartBlue / 255.0f, 0f, 1f);
                drawable.Colour = new Colour4(red, green, blue, 1.0f);
            }
            else
            {
                drawable.Colour = Colour4.White; // Default white (no tinting)
            }

            // Reset blending to default (will be overridden by Parameter commands if active)
            drawable.Blending = BlendingParameters.Mixture;
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

            storyboardContainer.Scale = new osuTK.Vector2(scale);
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
