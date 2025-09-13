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
        /// Only considers normal commands (not persistent ones) for end time
        /// </summary>
        /// <param name="sprite">The sprite to calculate lifetime for</param>
        /// <returns>Tuple containing start and end times</returns>
        public static (double Start, double End) GetSpriteLifetime(StoryboardSprite sprite)
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