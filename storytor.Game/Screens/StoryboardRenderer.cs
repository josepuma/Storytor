using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using storytor.Game.Storyboard.Models;

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
        private int updateCounter = 0;
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
            updateCounter++;

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
            var (start, end) = getSpriteLifetime(storyboardSprite);

            if (timeMs < start || timeMs > end)
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
                        var fadeCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<FadeCommand>(), timeMs);
                        if (fadeCmd != null)
                        {
                            var opacity = interpolateDoubleValue(timeMs, fadeCmd.StartTime, fadeCmd.EndTime, fadeCmd.StartOpacity, fadeCmd.EndOpacity, fadeCmd.Easing);
                            drawable.Alpha = Math.Clamp((float)opacity, 0f, 1f);
                        }
                        break;

                    case nameof(MoveCommand):
                        var moveCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<MoveCommand>(), timeMs);
                        if (moveCmd != null)
                        {
                            var xOffset = (854f - 640f) / 2f; // Same offset as initial position
                            var startPos = new osuTK.Vector2(moveCmd.StartX + xOffset, moveCmd.StartY);
                            var endPos = new osuTK.Vector2(moveCmd.EndX + xOffset, moveCmd.EndY);
                            var currentPos = interpolateVector2Value(timeMs, moveCmd.StartTime, moveCmd.EndTime, startPos, endPos, moveCmd.Easing);
                            drawable.Position = currentPos;
                        }
                        break;

                    case nameof(ScaleCommand):
                        var scaleCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<ScaleCommand>(), timeMs);
                        if (scaleCmd != null)
                        {
                            var scale = interpolateDoubleValue(timeMs, scaleCmd.StartTime, scaleCmd.EndTime, scaleCmd.StartScale, scaleCmd.EndScale, scaleCmd.Easing);
                            drawable.Scale = new osuTK.Vector2((float)scale);
                        }
                        break;

                    case nameof(VectorScaleCommand):
                        var vecScaleCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<VectorScaleCommand>(), timeMs);
                        if (vecScaleCmd != null)
                        {
                            var startScale = new osuTK.Vector2(vecScaleCmd.StartScaleX, vecScaleCmd.StartScaleY);
                            var endScale = new osuTK.Vector2(vecScaleCmd.EndScaleX, vecScaleCmd.EndScaleY);
                            var currentScale = interpolateVector2Value(timeMs, vecScaleCmd.StartTime, vecScaleCmd.EndTime, startScale, endScale, vecScaleCmd.Easing);
                            drawable.Scale = currentScale;
                        }
                        break;

                    case nameof(RotateCommand):
                        var rotateCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<RotateCommand>(), timeMs);
                        if (rotateCmd != null)
                        {
                            var angleRad = interpolateDoubleValue(timeMs, rotateCmd.StartTime, rotateCmd.EndTime, rotateCmd.StartAngle, rotateCmd.EndAngle, rotateCmd.Easing);
                            var angleDegrees = (float)(angleRad * 180.0 / Math.PI);
                            drawable.Rotation = angleDegrees;
                        }
                        break;


                    case nameof(MoveXCommand):
                        var moveXCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<MoveXCommand>(), timeMs);
                        if (moveXCmd != null)
                        {
                            var xOffset = (854f - 640f) / 2f;
                            var currentX = interpolateDoubleValue(timeMs, moveXCmd.StartTime, moveXCmd.EndTime, moveXCmd.StartX + xOffset, moveXCmd.EndX + xOffset, moveXCmd.Easing);
                            drawable.Position = new osuTK.Vector2((float)currentX, drawable.Position.Y);
                        }
                        break;

                    case nameof(MoveYCommand):
                        var moveYCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<MoveYCommand>(), timeMs);
                        if (moveYCmd != null)
                        {
                            var currentY = interpolateDoubleValue(timeMs, moveYCmd.StartTime, moveYCmd.EndTime, moveYCmd.StartY, moveYCmd.EndY, moveYCmd.Easing);
                            drawable.Position = new osuTK.Vector2(drawable.Position.X, (float)currentY);
                        }
                        break;

                    case nameof(ColorCommand):
                        var colorCmd = getActiveCommandOptimizedOptimized(commandsOfType.Cast<ColorCommand>(), timeMs);
                        if (colorCmd != null)
                        {
                            var currentRed = interpolateDoubleValue(timeMs, colorCmd.StartTime, colorCmd.EndTime, colorCmd.StartRed, colorCmd.EndRed, colorCmd.Easing);
                            var currentGreen = interpolateDoubleValue(timeMs, colorCmd.StartTime, colorCmd.EndTime, colorCmd.StartGreen, colorCmd.EndGreen, colorCmd.Easing);
                            var currentBlue = interpolateDoubleValue(timeMs, colorCmd.StartTime, colorCmd.EndTime, colorCmd.StartBlue, colorCmd.EndBlue, colorCmd.Easing);

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
                        var activeParameterCmds = getActiveParameterCommands(commandsOfType.Cast<ParameterCommand>(), timeMs, allCommands);

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

        private static (double Start, double End) getSpriteLifetime(StoryboardSprite sprite)
        {
            if (sprite.Commands.Count == 0)
                return (0, 0); // No commands = no lifetime

            var startTime = sprite.Commands.Min(c => c.StartTime);
            
            // Only consider normal commands (not persistent ones) for end time
            var normalCommands = sprite.Commands.Where(c => c.EndTime != int.MaxValue).ToList();
            
            double endTime;
            if (normalCommands.Count > 0)
            {
                // Use the latest end time from normal commands
                endTime = normalCommands.Max(c => c.EndTime);
            }
            else
            {
                // All commands are persistent - sprite should be visible from first start time onward
                // But this is unusual, typically there should be at least one normal command
                endTime = startTime; // Minimal lifetime
            }

            return (startTime, endTime);
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


        /// <summary>
        /// Generic function to get the active command at a given time
        /// </summary>
        private static T getActiveCommandOptimized<T>(IEnumerable<T> commands, double timeMs) where T : StoryboardCommand
        {
            // Find active command (currently executing)
            var activeCommand = commands
                .Where(cmd => timeMs >= cmd.StartTime && timeMs <= cmd.EndTime)
                .OrderByDescending(cmd => cmd.StartTime)
                .FirstOrDefault();

            if (activeCommand != null)
                return activeCommand;

            // If no active command, use the end state of the last completed command
            var lastCompletedCommand = commands
                .Where(cmd => timeMs > cmd.EndTime)
                .OrderByDescending(cmd => cmd.EndTime)
                .FirstOrDefault();

            return lastCompletedCommand;
        }

        private static T getActiveCommandOptimizedOptimized<T>(IEnumerable<T> commands, double timeMs) where T : StoryboardCommand
        {
            var commandList = commands as List<T> ?? commands.ToList();
            if (commandList.Count == 0) return null;

            // Sort commands by start time to process in chronological order
            var sortedCommands = commandList.OrderBy(c => c.StartTime).ToList();
            
            T activeCommand = null;
            
            // Process commands chronologically to find what should be active at timeMs
            for (int i = 0; i < sortedCommands.Count; i++)
            {
                var cmd = sortedCommands[i];
                
                if (cmd.EndTime == int.MaxValue)
                {
                    // Persistent command: starts at StartTime and continues until overridden
                    if (timeMs >= cmd.StartTime)
                    {
                        activeCommand = cmd;
                        
                        // Check if this persistent command is later overridden by any command
                        for (int j = i + 1; j < sortedCommands.Count; j++)
                        {
                            var laterCmd = sortedCommands[j];
                            if (timeMs >= laterCmd.StartTime)
                            {
                                // This later command overrides the persistent one
                                activeCommand = laterCmd;
                                // Don't break here, continue checking for even later commands
                            }
                            else
                            {
                                // We've reached commands that haven't started yet
                                break;
                            }
                        }
                        break; // We found our persistent command and checked all overrides
                    }
                }
                else
                {
                    // Normal command: active within its time range
                    if (timeMs >= cmd.StartTime && timeMs <= cmd.EndTime)
                    {
                        activeCommand = cmd;
                        // Don't break, continue checking for later commands that might override
                    }
                    else if (timeMs > cmd.EndTime)
                    {
                        // Command has ended, keep it as potential fallback
                        activeCommand = cmd;
                    }
                }
            }

            return activeCommand;
        }

        /// <summary>
        /// Gets active parameter commands, handling persistent parameters correctly
        /// Single persistent parameters apply for the entire sprite lifetime, ignoring their StartTime
        /// </summary>
        private static List<ParameterCommand> getActiveParameterCommands(IEnumerable<ParameterCommand> parameterCommands, double timeMs, IEnumerable<StoryboardCommand> allCommands)
        {
            var paramList = parameterCommands.ToList();
            var activeParams = new List<ParameterCommand>();
            
            if (!paramList.Any()) return activeParams;
            
            // Calculate sprite's lifetime from all commands (excluding parameters)
            var nonParamCommands = allCommands.Where(c => !(c is ParameterCommand)).ToList();
            if (!nonParamCommands.Any()) return activeParams;
            
            var spriteStartTime = nonParamCommands.Min(c => c.StartTime);
            var spriteEndTime = nonParamCommands.Max(c => c.EndTime);
            
            // Group parameters by type
            var paramGroups = paramList.GroupBy(p => p.Parameter);
            
            foreach (var group in paramGroups)
            {
                var paramsOfType = group.OrderBy(p => p.StartTime).ToList();
                
                // Special case: if there's only one parameter of this type and it's persistent,
                // it applies for the entire sprite lifetime (ignoring StartTime)
                if (paramsOfType.Count == 1 && paramsOfType[0].EndTime == int.MaxValue)
                {
                    // Single persistent parameter: active during entire sprite lifetime
                    if (timeMs >= spriteStartTime && timeMs <= spriteEndTime)
                    {
                        activeParams.Add(paramsOfType[0]);
                    }
                }
                else
                {
                    // Multiple parameters or non-persistent: use normal timing logic
                    foreach (var param in paramsOfType)
                    {
                        bool isActive = false;
                        
                        if (param.EndTime == int.MaxValue)
                        {
                            // Persistent parameter: active from StartTime until replaced or sprite ends
                            if (timeMs >= param.StartTime && timeMs <= spriteEndTime)
                            {
                                // Check if replaced by a later parameter
                                var laterParam = paramsOfType
                                    .FirstOrDefault(p => p.StartTime > param.StartTime && timeMs >= p.StartTime);
                                
                                isActive = (laterParam == null);
                            }
                        }
                        else
                        {
                            // Normal parameter: active within its time range
                            isActive = (timeMs >= param.StartTime && timeMs <= param.EndTime);
                        }
                        
                        if (isActive)
                        {
                            activeParams.Add(param);
                            break; // Only one parameter of each type can be active
                        }
                    }
                }
            }
            
            return activeParams;
        }

        /// <summary>
        /// Generic interpolation function for commands with double values
        /// </summary>
        private static double interpolateDoubleValue(double timeMs, int startTime, int endTime, double startValue, double endValue, int easing)
        {
            if (timeMs < startTime) return startValue;
            if (timeMs >= endTime) return endValue;

            var progress = (timeMs - startTime) / (endTime - startTime);
            var easedProgress = applyEasing(progress, easing);
            return startValue + (endValue - startValue) * easedProgress;
        }

        /// <summary>
        /// Generic interpolation function for commands with Vector2 values
        /// </summary>
        private static osuTK.Vector2 interpolateVector2Value(double timeMs, int startTime, int endTime,
            osuTK.Vector2 startValue, osuTK.Vector2 endValue, int easing)
        {
            if (timeMs < startTime) return startValue;
            if (timeMs >= endTime) return endValue;

            var progress = (timeMs - startTime) / (endTime - startTime);
            var easedProgress = applyEasing(progress, easing);
            return startValue + (endValue - startValue) * (float)easedProgress;
        }

        private static double applyEasing(double progress, int easingType)
        {
            return easingType switch
            {
                0 => progress, // Linear
                1 => 1 - Math.Pow(1 - progress, 2), // Out
                2 => Math.Pow(progress, 2), // In
                3 => progress < 0.5 ? 2 * progress * progress : 1 - Math.Pow(-2 * progress + 2, 2) / 2, // InOut
                _ => progress // Default to linear
            };
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
