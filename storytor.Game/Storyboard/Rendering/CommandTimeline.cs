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

        // Internal access for optimization
        internal IReadOnlyList<StoryboardCommand> Commands => commands;

        // Cache for performance optimization
        private double lastQueryTime = double.MinValue;
        private StoryboardCommand lastActiveCommand = null;
        private int lastSearchIndex = 0;

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

            // Invalidate cache when commands change
            InvalidateCache();
        }
        
        public void Clear()
        {
            commands.Clear();
            InvalidateCache();
        }

        private void InvalidateCache()
        {
            lastQueryTime = double.MinValue;
            lastActiveCommand = null;
            lastSearchIndex = 0;
        }
        
        /// <summary>
        /// Gets the active command at a specific time (optimized with caching)
        /// </summary>
        public StoryboardCommand GetActiveCommand(double time)
        {
            if (!HasCommands) return null;

            // Use cache if querying the same time or very close time
            if (Math.Abs(time - lastQueryTime) < 1.0) // Within 1ms tolerance
            {
                if (lastActiveCommand != null &&
                    time >= lastActiveCommand.StartTime &&
                    time <= lastActiveCommand.EndTime)
                {
                    return lastActiveCommand;
                }
            }

            // Cache miss - do optimized search
            StoryboardCommand activeCommand = null;
            int startIndex = 0;

            // If time is close to last query, start search from last position
            if (time >= lastQueryTime && lastSearchIndex < commands.Count)
            {
                startIndex = Math.Max(0, lastSearchIndex - 1);
            }

            // Binary search for better performance with many commands
            if (commands.Count > 20) // Use binary search for large command lists
            {
                int left = startIndex;
                int right = commands.Count - 1;
                int bestIndex = -1;

                while (left <= right)
                {
                    int mid = (left + right) / 2;
                    if (commands[mid].StartTime <= time)
                    {
                        bestIndex = mid;
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }

                // Check commands around the found index
                for (int i = Math.Max(0, bestIndex - 2); i <= Math.Min(commands.Count - 1, bestIndex + 2); i++)
                {
                    var command = commands[i];
                    if (time >= command.StartTime && time <= command.EndTime)
                    {
                        activeCommand = command;
                        lastSearchIndex = i;
                    }
                }
            }
            else
            {
                // Linear search for small command lists
                for (int i = startIndex; i < commands.Count; i++)
                {
                    var command = commands[i];
                    if (time < command.StartTime) break;

                    if (time >= command.StartTime && time <= command.EndTime)
                    {
                        activeCommand = command;
                        lastSearchIndex = i;
                    }
                }
            }

            // Update cache
            lastQueryTime = time;
            lastActiveCommand = activeCommand;

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

            // If no active command, find the last command that ended before this time
            StoryboardCommand lastCommand = null;
            foreach (var cmd in commands)
            {
                if (cmd.EndTime < time)
                    lastCommand = cmd;
                else
                    break;
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

            // If no active command, find the last command that ended before this time
            StoryboardCommand lastCommand = null;
            foreach (var cmd in commands)
            {
                if (cmd.EndTime < time)
                    lastCommand = cmd;
                else
                    break;
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

            // If no active command, find the last command that ended before this time
            StoryboardCommand lastCommand = null;
            foreach (var cmd in commands)
            {
                if (cmd.EndTime < time)
                    lastCommand = cmd;
                else
                    break;
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

            foreach (var command in commands)
            {
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