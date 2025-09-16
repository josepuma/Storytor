using System;
using System.Collections.Generic;
using System.Linq;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Rendering
{
    /// <summary>
    /// Timeline that manages commands of a specific type and provides value interpolation
    /// </summary>
    public class CommandTimeline
    {
        private readonly List<StoryboardCommand> commands = new List<StoryboardCommand>();
        private readonly string commandType;

        public bool HasCommands => commands.Count > 0;

        public CommandTimeline(string commandType)
        {
            this.commandType = commandType;
        }

        public void AddCommand(StoryboardCommand command)
        {
            if (command.CommandType != commandType) return;

            commands.Add(command);
            commands.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        public void Clear()
        {
            commands.Clear();
        }

        /// <summary>
        /// Gets the active command at a specific time using binary search
        /// </summary>
        public StoryboardCommand GetActiveCommand(double time)
        {
            if (!HasCommands) return null;

            // Binary search to find the insertion point
            int left = 0;
            int right = commands.Count - 1;
            StoryboardCommand activeCommand = null;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var command = commands[mid];

                if (time >= command.StartTime && time <= command.EndTime)
                {
                    // Found an active command, but there might be a later one that's also active
                    activeCommand = command;
                    left = mid + 1; // Continue searching for a later active command
                }
                else if (time < command.StartTime)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return activeCommand;
        }

        /// <summary>
        /// Gets the interpolated single value at a specific time
        /// </summary>
        public double GetValueAt(double time, double defaultValue = 0.0)
        {
            var command = GetActiveCommand(time);
            if (command != null)
                return command.GetValueAt(time);

            // If no active command, find the last command that ended before this time using binary search
            StoryboardCommand lastCommand = null;
            int left = 0;
            int right = commands.Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var cmd = commands[mid];

                if (cmd.EndTime < time)
                {
                    lastCommand = cmd;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // Use the end value of the last command, or start value of first command, or default
            if (lastCommand != null)
                return lastCommand.EndValue;

            // If time is before all commands, use the start value of the first command
            if (commands.Count > 0 && time < commands[0].StartTime)
                return commands[0].StartValue;

            return defaultValue;
        }

        /// <summary>
        /// Gets the interpolated position at a specific time (for M, V commands)
        /// </summary>
        public (double x, double y) GetPositionAt(double time, double defaultX = 0.0, double defaultY = 0.0)
        {
            var command = GetActiveCommand(time);
            if (command != null)
                return command.GetPositionAt(time);

            // If no active command, find the last command that ended before this time using binary search
            StoryboardCommand lastCommand = null;
            int left = 0;
            int right = commands.Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var cmd = commands[mid];

                if (cmd.EndTime < time)
                {
                    lastCommand = cmd;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // Use the end values of the last command, or start values of first command, or defaults
            if (lastCommand != null)
                return (lastCommand.EndX, lastCommand.EndY);

            // If time is before all commands, use the start values of the first command
            if (commands.Count > 0 && time < commands[0].StartTime)
                return (commands[0].StartX, commands[0].StartY);

            return (defaultX, defaultY);
        }

        /// <summary>
        /// Gets the interpolated color at a specific time (for C commands)
        /// </summary>
        public (double red, double green, double blue) GetColorAt(double time, double defaultRed = 255.0, double defaultGreen = 255.0, double defaultBlue = 255.0)
        {
            var command = GetActiveCommand(time);
            if (command != null)
                return command.GetColorAt(time);

            // If no active command, find the last command that ended before this time using binary search
            StoryboardCommand lastCommand = null;
            int left = 0;
            int right = commands.Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var cmd = commands[mid];

                if (cmd.EndTime < time)
                {
                    lastCommand = cmd;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // Use the end values of the last command, or start values of first command, or defaults
            if (lastCommand != null)
                return (lastCommand.EndRed, lastCommand.EndGreen, lastCommand.EndBlue);

            // If time is before all commands, use the start values of the first command
            if (commands.Count > 0 && time < commands[0].StartTime)
                return (commands[0].StartRed, commands[0].StartGreen, commands[0].StartBlue);

            return (defaultRed, defaultGreen, defaultBlue);
        }

        /// <summary>
        /// Gets the parameter type at a specific time (for P commands)
        /// </summary>
        public string GetParameterAt(double time)
        {
            var command = GetActiveCommand(time);
            return command?.ParameterType ?? "";
        }

        /// <summary>
        /// Gets all active parameters at a specific time (multiple P commands can be active)
        /// Special logic for P commands: if StartTime == EndTime, parameter applies for sprite's lifetime
        /// </summary>
        public List<string> GetActiveParametersAt(double time)
        {
            var activeParams = new List<string>();

            // Use binary search to find starting point
            int left = 0;
            int right = commands.Count - 1;
            int startIndex = -1; // Start from -1 if no valid command found

            // Find last command where StartTime <= time
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (commands[mid].StartTime <= time)
                {
                    startIndex = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // If no valid starting point found, return empty list
            if (startIndex == -1) return activeParams;

            // Check all commands from startIndex backwards for active parameters
            for (int i = startIndex; i >= 0; i--)
            {
                var command = commands[i];
                if (string.IsNullOrEmpty(command.ParameterType)) continue;

                // Special case: if StartTime == EndTime, parameter applies from StartTime onwards
                if (command.StartTime == command.EndTime)
                {
                    if (time >= command.StartTime)
                    {
                        activeParams.Add(command.ParameterType);
                    }
                }
                else
                {
                    // Normal case: parameter active only within time range
                    if (time >= command.StartTime && time <= command.EndTime)
                    {
                        activeParams.Add(command.ParameterType);
                    }
                }

                // Early exit if we've gone past relevant commands
                if (command.EndTime < time && command.StartTime != command.EndTime)
                    break;
            }

            return activeParams;
        }

        /// <summary>
        /// Calculates when the sprite should start being visible based on this timeline
        /// </summary>
        public double? GetDisplayStartTime()
        {
            if (!HasCommands) return null;

            // For fade commands, find when fade becomes > 0
            if (commandType == "F")
            {
                foreach (var command in commands)
                {
                    if (command.StartValue > 0 || command.EndValue > 0)
                        return command.StartTime;
                }
                return null; // Never visible
            }

            // For scale commands, find when scale becomes > 0
            if (commandType == "S" || commandType == "V")
            {
                foreach (var command in commands)
                {
                    var startVisible = commandType == "S" ? command.StartValue > 0 :
                                      (command.StartX > 0 && command.StartY > 0);
                    var endVisible = commandType == "S" ? command.EndValue > 0 :
                                    (command.EndX > 0 && command.EndY > 0);

                    if (startVisible || endVisible)
                        return command.StartTime;
                }
                return null; // Never visible
            }

            return commands.First().StartTime;
        }

        /// <summary>
        /// Calculates when the sprite should stop being visible based on this timeline
        /// </summary>
        public double? GetDisplayEndTime()
        {
            if (!HasCommands) return null;

            // For fade commands, find when fade becomes 0 and stays 0
            if (commandType == "F")
            {
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    var command = commands[i];
                    if (command.StartValue > 0 || command.EndValue > 0)
                        return command.EndTime;
                }
                return commands.First().StartTime; // Immediately invisible
            }

            // For scale commands, similar logic
            if (commandType == "S" || commandType == "V")
            {
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    var command = commands[i];
                    var startVisible = commandType == "S" ? command.StartValue > 0 :
                                      (command.StartX > 0 && command.StartY > 0);
                    var endVisible = commandType == "S" ? command.EndValue > 0 :
                                    (command.EndX > 0 && command.EndY > 0);

                    if (startVisible || endVisible)
                        return command.EndTime;
                }
                return commands.First().StartTime; // Immediately invisible
            }

            return commands.Last().EndTime;
        }
    }
}
