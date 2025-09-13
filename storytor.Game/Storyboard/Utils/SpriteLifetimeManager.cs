using System;
using System.Linq;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Utils
{
    /// <summary>
    /// Manages sprite lifetime calculations
    /// </summary>
    public static class SpriteLifetimeManager
    {
        /// <summary>
        /// Calculates the lifetime of a sprite based on its commands
        /// Sprite lives from earliest StartTime to latest relevant time
        /// </summary>
        /// <param name="sprite">The sprite to calculate lifetime for</param>
        /// <returns>Tuple containing start and end times</returns>
        public static (double Start, double End) GetSpriteLifetime(StoryboardSprite sprite)
        {
            if (sprite.Commands.Count == 0)
                return (0, 0); // No commands = no lifetime

            var startTime = sprite.Commands.Min(c => c.StartTime);

            // Calculate end time: latest StartTime OR latest EndTime from normal commands
            var normalCommands = sprite.Commands.Where(c => c.EndTime != int.MaxValue).ToList();
            var latestStartTime = sprite.Commands.Max(c => c.StartTime);

            double endTime;
            if (normalCommands.Count > 0)
            {
                // Use the later of: latest normal command EndTime OR latest StartTime
                var latestEndTime = normalCommands.Max(c => c.EndTime);
                endTime = Math.Max(latestEndTime, latestStartTime);
            }
            else
            {
                // All commands are persistent - use latest StartTime
                endTime = latestStartTime;
            }

            return (startTime, endTime);
        }

        /// <summary>
        /// Checks if a sprite should be visible at the given time
        /// </summary>
        /// <param name="sprite">The sprite to check</param>
        /// <param name="timeMs">Current time in milliseconds</param>
        /// <returns>True if sprite should be visible</returns>
        public static bool IsSpriteVisible(StoryboardSprite sprite, double timeMs)
        {
            var (start, end) = GetSpriteLifetime(sprite);
            return timeMs >= start && timeMs <= end;
        }
    }
}
