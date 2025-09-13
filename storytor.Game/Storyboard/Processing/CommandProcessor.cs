using System.Collections.Generic;
using System.Linq;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Processing
{
    /// <summary>
    /// Handles command processing and selection logic
    /// </summary>
    public static class CommandProcessor
    {
        /// <summary>
        /// Gets the active command at a given time, handling both persistent and normal commands
        /// </summary>
        /// <typeparam name="T">Type of command</typeparam>
        /// <param name="commands">List of commands to search</param>
        /// <param name="timeMs">Current time in milliseconds</param>
        /// <returns>The most relevant command for the current time, or null if none</returns>
        public static T GetActiveCommand<T>(IEnumerable<T> commands, double timeMs) where T : StoryboardCommand
        {
            var commandList = commands as List<T> ?? commands.ToList();
            if (commandList.Count == 0) return null;

            // Sort commands by start time to process in chronological order
            var sortedCommands = commandList.OrderBy(c => c.StartTime).ToList();

            T bestCommand = null;

            // Find the most relevant command for the current time
            // Priority: Active commands > Most recent started command
            foreach (var cmd in sortedCommands)
            {
                if (cmd.StartTime > timeMs)
                {
                    // Command hasn't started yet, skip
                    continue;
                }

                if (cmd.EndTime == int.MaxValue)
                {
                    // Persistent command: active from StartTime until overridden by a later command
                    bestCommand = cmd;

                    // Check if there's a later command that overrides this persistent one
                    var overridingCmd = sortedCommands
                        .Where(c => c.StartTime > cmd.StartTime && c.StartTime <= timeMs)
                        .OrderByDescending(c => c.StartTime)
                        .FirstOrDefault();

                    if (overridingCmd != null)
                    {
                        bestCommand = overridingCmd;
                    }
                }
                else
                {
                    // Normal command: Use it regardless of whether it's still active
                    // This ensures that completed commands maintain their final values
                    bestCommand = cmd;
                }
            }

            return bestCommand;
        }

        /// <summary>
        /// Gets active parameter commands, handling persistent parameters correctly
        /// Single persistent parameters apply for the entire sprite lifetime, ignoring their StartTime
        /// </summary>
        public static List<ParameterCommand> GetActiveParameterCommands(IEnumerable<ParameterCommand> commands, double timeMs, double spriteStartTime, double spriteEndTime)
        {
            var commandList = commands as List<ParameterCommand> ?? commands.ToList();
            if (commandList.Count == 0) return new List<ParameterCommand>();

            var activeCommands = new List<ParameterCommand>();

            // Group commands by parameter type
            var commandsByType = commandList.GroupBy(c => c.Parameter);

            foreach (var group in commandsByType)
            {
                var typeCommands = group.OrderBy(c => c.StartTime).ToList();

                if (typeCommands.Count == 1 && typeCommands[0].EndTime == int.MaxValue)
                {
                    // Single persistent parameter: applies for entire sprite lifetime
                    if (timeMs >= spriteStartTime && timeMs <= spriteEndTime)
                    {
                        activeCommands.Add(typeCommands[0]);
                    }
                }
                else
                {
                    // Multiple parameters or non-persistent: use normal logic
                    if (GetActiveCommand(typeCommands.Cast<StoryboardCommand>(), timeMs) is ParameterCommand activeParam)
                    {
                        activeCommands.Add(activeParam);
                    }
                }
            }

            return activeCommands;
        }
    }
}
