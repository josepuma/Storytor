using System;
using System.Collections.Generic;
using System.Linq;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Rendering
{
    /// <summary>
    /// Manages all command timelines for a single sprite
    /// </summary>
    public class SpriteTimelineManager
    {
        private readonly Dictionary<string, CommandTimeline> timelines;
        private readonly StoryboardSprite sprite;
        
        public double CommandsStartTime { get; private set; } = double.MaxValue;
        public double CommandsEndTime { get; private set; } = double.MinValue;
        public double DisplayStartTime { get; private set; } = double.MaxValue;
        public double DisplayEndTime { get; private set; } = double.MinValue;
        
        public SpriteTimelineManager(StoryboardSprite sprite)
        {
            this.sprite = sprite;
            
            // Initialize timelines for all command types
            timelines = new Dictionary<string, CommandTimeline>
            {
                ["F"] = new CommandTimeline("F"),   // Fade
                ["M"] = new CommandTimeline("M"),   // Move
                ["MX"] = new CommandTimeline("MX"), // MoveX  
                ["MY"] = new CommandTimeline("MY"), // MoveY
                ["S"] = new CommandTimeline("S"),   // Scale
                ["V"] = new CommandTimeline("V"),   // Vector Scale
                ["R"] = new CommandTimeline("R"),   // Rotate
                ["C"] = new CommandTimeline("C"),   // Color
                ["P"] = new CommandTimeline("P")    // Parameter
            };
            
            BuildTimelines();
            CalculateDisplayTimes();
        }
        
        private void BuildTimelines()
        {
            foreach (var command in sprite.Commands)
            {
                if (command.IsLoop)
                {
                    // Expand loop commands during rendering
                    ExpandAndAddLoopCommand(command);
                }
                else if (timelines.TryGetValue(command.CommandType, out var timeline))
                {
                    timeline.AddCommand(command);

                    // Update command time bounds
                    CommandsStartTime = Math.Min(CommandsStartTime, command.StartTime);
                    CommandsEndTime = Math.Max(CommandsEndTime, command.EndTime);
                }
            }
            
            // If no commands, use sprite as point in time
            if (CommandsStartTime == double.MaxValue)
            {
                CommandsStartTime = 0;
                CommandsEndTime = 0;
            }
        }
        
        private void CalculateDisplayTimes()
        {
            // Sprite is active from first command start to last command end
            DisplayStartTime = CommandsStartTime;
            DisplayEndTime = CommandsEndTime;

            // The sprite should only be visible during the time range of its commands
            // No additional refinement needed - visibility is determined by fade/scale values at render time
        }
        
        /// <summary>
        /// Gets the sprite state at a specific time
        /// </summary>
        public SpriteState GetStateAt(double time, float xOffset = 0f)
        {
            return SpriteState.FromTimelines(sprite, timelines, time, xOffset);
        }
        
        /// <summary>
        /// Checks if the sprite is active (has commands) at a specific time
        /// </summary>
        public bool IsActiveAt(double time)
        {
            return time >= CommandsStartTime && time <= CommandsEndTime;
        }
        
        /// <summary>
        /// Checks if the sprite should be visible at a specific time
        /// </summary>
        public bool IsVisibleAt(double time)
        {
            return time >= DisplayStartTime && time <= DisplayEndTime && GetStateAt(time).IsVisible;
        }
        
        /// <summary>
        /// Gets a timeline for a specific command type
        /// </summary>
        public CommandTimeline GetTimeline(string commandType)
        {
            return timelines.GetValueOrDefault(commandType);
        }
        
        /// <summary>
        /// Checks if the sprite has any commands of a specific type
        /// </summary>
        public bool HasCommands(string commandType)
        {
            return timelines.GetValueOrDefault(commandType)?.HasCommands == true;
        }
        
        /// <summary>
        /// Gets all command types that have commands
        /// </summary>
        public IEnumerable<string> GetActiveCommandTypes()
        {
            return timelines.Where(kvp => kvp.Value.HasCommands).Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Expands a loop command and adds the expanded commands to timelines
        /// </summary>
        private void ExpandAndAddLoopCommand(StoryboardCommand loopCommand)
        {
            if (loopCommand.LoopCommands.Count == 0 || loopCommand.LoopCount <= 0) return;

            // Calculate loop duration from the commands
            double loopDuration = 0;
            foreach (var cmd in loopCommand.LoopCommands)
            {
                loopDuration = Math.Max(loopDuration, cmd.EndTime);
            }

            // Create expanded commands for each loop iteration
            for (int i = 0; i < loopCommand.LoopCount; i++)
            {
                double timeOffset = loopCommand.StartTime + (i * loopDuration);

                foreach (var originalCmd in loopCommand.LoopCommands)
                {
                    // Create a copy of the command with adjusted timing
                    var expandedCmd = new StoryboardCommand
                    {
                        CommandType = originalCmd.CommandType,
                        Easing = originalCmd.Easing,
                        StartTime = originalCmd.StartTime + timeOffset,
                        EndTime = originalCmd.EndTime + timeOffset,

                        // Copy all value properties
                        StartValue = originalCmd.StartValue,
                        EndValue = originalCmd.EndValue,
                        StartX = originalCmd.StartX,
                        StartY = originalCmd.StartY,
                        EndX = originalCmd.EndX,
                        EndY = originalCmd.EndY,
                        StartRed = originalCmd.StartRed,
                        StartGreen = originalCmd.StartGreen,
                        StartBlue = originalCmd.StartBlue,
                        EndRed = originalCmd.EndRed,
                        EndGreen = originalCmd.EndGreen,
                        EndBlue = originalCmd.EndBlue,
                        ParameterType = originalCmd.ParameterType
                    };

                    // Add to appropriate timeline
                    if (timelines.TryGetValue(expandedCmd.CommandType, out var timeline))
                    {
                        timeline.AddCommand(expandedCmd);

                        // Update command time bounds
                        CommandsStartTime = Math.Min(CommandsStartTime, expandedCmd.StartTime);
                        CommandsEndTime = Math.Max(CommandsEndTime, expandedCmd.EndTime);
                    }
                }
            }
        }
    }
}