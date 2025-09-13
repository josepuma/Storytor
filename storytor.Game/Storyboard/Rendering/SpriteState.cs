using System.Collections.Generic;
using System.Threading;
using osuTK;
using osu.Framework.Graphics;

namespace storytor.Game.Storyboard.Rendering
{
    /// <summary>
    /// Represents the visual state of a sprite at a specific time
    /// </summary>
    public class SpriteState
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Scale { get; set; } = Vector2.One;
        public float Rotation { get; set; } = 0f;
        public float Alpha { get; set; } = 1f;
        public Colour4 Color { get; set; } = Colour4.White;
        public bool FlipH { get; set; } = false;
        public bool FlipV { get; set; } = false;
        public bool Additive { get; set; } = false;
        public bool IsVisible { get; set; } = true;

        // Reusable instances to reduce allocations
        private static readonly ThreadLocal<SpriteState> _cachedState = new(() => new SpriteState());
        private static readonly ThreadLocal<List<string>> _cachedParameterList = new(() => new List<string>());
        
        /// <summary>
        /// Creates a sprite state from initial sprite properties and timelines at a specific time (optimized)
        /// </summary>
        public static SpriteState FromTimelines(
            Models.StoryboardSprite sprite,
            Dictionary<string, CommandTimeline> timelines,
            double time,
            float xOffset = 0f)
        {
            // Use cached instance to avoid allocation
            var state = _cachedState.Value;

            // Reset state to defaults
            state.Position = Vector2.Zero;
            state.Scale = Vector2.One;
            state.Rotation = 0f;
            state.Alpha = 1f;
            state.Color = Colour4.White;
            state.FlipH = false;
            state.FlipV = false;
            state.Additive = false;
            state.IsVisible = true;
            
            // Position - prioritize MX/MY over M
            var moveTimeline = timelines.GetValueOrDefault("M");
            var moveXTimeline = timelines.GetValueOrDefault("MX");
            var moveYTimeline = timelines.GetValueOrDefault("MY");
            
            float x = sprite.X;
            float y = sprite.Y;
            
            if (moveXTimeline?.HasCommands == true)
                x = (float)moveXTimeline.GetValueAt(time, sprite.X);
            else if (moveTimeline?.HasCommands == true)
                (x, y) = ((float, float))moveTimeline.GetPositionAt(time, sprite.X, sprite.Y);
            
            if (moveYTimeline?.HasCommands == true)
                y = (float)moveYTimeline.GetValueAt(time, sprite.Y);
            else if (moveTimeline?.HasCommands == true && moveXTimeline?.HasCommands != true)
                (x, y) = ((float, float))moveTimeline.GetPositionAt(time, sprite.X, sprite.Y);
            
            state.Position = new Vector2(x + xOffset, y);
            
            // Scale - prioritize V over S, default to 1.0
            var scaleTimeline = timelines.GetValueOrDefault("S");
            var vectorScaleTimeline = timelines.GetValueOrDefault("V");

            if (vectorScaleTimeline?.HasCommands == true)
            {
                var (scaleX, scaleY) = vectorScaleTimeline.GetPositionAt(time, 1.0, 1.0);
                state.Scale = new Vector2((float)scaleX, (float)scaleY);
            }
            else if (scaleTimeline?.HasCommands == true)
            {
                var scale = scaleTimeline.GetValueAt(time, 1.0);
                state.Scale = new Vector2((float)scale);
            }
            else
            {
                state.Scale = Vector2.One; // Default scale
            }
            
            // Rotation (osu! uses radians, Framework uses degrees)
            var rotateTimeline = timelines.GetValueOrDefault("R");
            if (rotateTimeline?.HasCommands == true)
            {
                var rotationRad = rotateTimeline.GetValueAt(time, 0.0);
                state.Rotation = (float)(rotationRad * 180.0 / System.Math.PI);
            }
            
            // Alpha/Opacity - default to 1.0 if no fade commands, otherwise use timeline value
            var fadeTimeline = timelines.GetValueOrDefault("F");
            var alphaValue = fadeTimeline?.HasCommands == true
                ? fadeTimeline.GetValueAt(time, 1.0)  // Use timeline with persistence logic
                : 1.0; // No fade commands at all, default to visible
            state.Alpha = (float)System.Math.Clamp(alphaValue, 0.0, 1.0);
            
            // Color
            var colorTimeline = timelines.GetValueOrDefault("C");
            if (colorTimeline?.HasCommands == true)
            {
                var (r, g, b) = colorTimeline.GetColorAt(time, 255, 255, 255);
                var red = (float)System.Math.Clamp(r / 255.0, 0.0, 1.0);
                var green = (float)System.Math.Clamp(g / 255.0, 0.0, 1.0);
                var blue = (float)System.Math.Clamp(b / 255.0, 0.0, 1.0);
                state.Color = new Colour4(red, green, blue, 1.0f);
            }
            
            // Parameters - use cached list to avoid allocation
            var paramTimeline = timelines.GetValueOrDefault("P");
            if (paramTimeline?.HasCommands == true)
            {
                var activeParams = _cachedParameterList.Value;
                activeParams.Clear();

                // Get active parameters without creating new list
                GetActiveParametersAtOptimized(paramTimeline, time, activeParams);

                state.FlipH = activeParams.Contains("H");
                state.FlipV = activeParams.Contains("V");
                state.Additive = activeParams.Contains("A");
            }
            
            // Visibility calculation
            state.IsVisible = CalculateVisibility(timelines, time);
            
            return state;
        }
        
        private static bool CalculateVisibility(Dictionary<string, CommandTimeline> timelines, double time)
        {
            // Check fade timeline
            var fadeTimeline = timelines.GetValueOrDefault("F");
            if (fadeTimeline?.HasCommands == true)
            {
                var alpha = fadeTimeline.GetValueAt(time, 1.0);
                if (alpha <= 0) return false;
            }

            // Check scale timelines
            var scaleTimeline = timelines.GetValueOrDefault("S");
            var vectorScaleTimeline = timelines.GetValueOrDefault("V");

            if (vectorScaleTimeline?.HasCommands == true)
            {
                var (scaleX, scaleY) = vectorScaleTimeline.GetPositionAt(time, 1.0, 1.0);
                if (scaleX <= 0 || scaleY <= 0) return false;
            }
            else if (scaleTimeline?.HasCommands == true)
            {
                var scale = scaleTimeline.GetValueAt(time, 1.0);
                if (scale <= 0) return false;
            }

            return true;
        }

        /// <summary>
        /// Optimized version of GetActiveParametersAt that reuses a provided list
        /// </summary>
        private static void GetActiveParametersAtOptimized(CommandTimeline paramTimeline, double time, List<string> resultList)
        {
            // This replicates the logic from CommandTimeline.GetActiveParametersAt
            // but avoids creating a new list every time
            foreach (var command in paramTimeline.Commands)
            {
                if (string.IsNullOrEmpty(command.ParameterType)) continue;

                // Special case: if StartTime == EndTime, parameter applies from StartTime onwards
                if (command.StartTime == command.EndTime)
                {
                    if (time >= command.StartTime)
                    {
                        resultList.Add(command.ParameterType);
                    }
                }
                else
                {
                    // Normal case: parameter active only within time range
                    if (time >= command.StartTime && time <= command.EndTime)
                    {
                        resultList.Add(command.ParameterType);
                    }
                }
            }
        }
    }
}