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
        public static T GetActiveCommand<T>(IEnumerable<T> commands, double timeMs)
        where T : StoryboardCommand
        {
            if (commands == null) return null;

            var list = commands as IList<T> ?? [.. commands];
            if (list.Count == 0) return null;

            int left = 0;
            int right = list.Count - 1;
            int bestIndex = -1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                if (list[mid].StartTime <= timeMs)
                {
                    bestIndex = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (bestIndex == -1)
                return null;

            return list[bestIndex];
        }


        /// <summary>
        /// Gets active parameter commands, handling persistent parameters correctly
        /// Single persistent parameters apply for the entire sprite lifetime, ignoring their StartTime
        /// </summary>
        public static List<ParameterCommand> GetActiveParameterCommands(
            IEnumerable<ParameterCommand> commands,
            double timeMs)
        {
            var result = new List<ParameterCommand>();

            foreach (var cmd in commands)
            {
                if (cmd.StartTime == cmd.EndTime)
                {
                    // Parámetro constante, siempre aplica
                    result.Add(cmd);
                }
                else if (timeMs >= cmd.StartTime && timeMs <= cmd.EndTime)
                {
                    // Activo solo dentro del rango
                    result.Add(cmd);
                }
            }

            return result;
        }

    }
}
