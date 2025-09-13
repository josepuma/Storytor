
using System;
using System.Collections.Generic;
using System.Linq;

namespace storytor.Game.Storyboard.Models
{
    /// <summary>
    /// Represents a command that can be applied to a storyboard sprite
    /// </summary>
    public abstract class StoryboardCommand
    {
        /// <summary>
        /// The type of command (e.g., "F" for Fade)
        /// </summary>
        public string CommandType { get; set; } = string.Empty;
        
        /// <summary>
        /// Easing type for the command (0 = None, 1 = Out, 2 = In, 3 = InOut)
        /// </summary>
        public int Easing { get; set; }
        
        /// <summary>
        /// Start time of the command in milliseconds
        /// </summary>
        public int StartTime { get; set; }
        
        /// <summary>
        /// End time of the command in milliseconds
        /// </summary>
        public int EndTime { get; set; }
    }
    
    /// <summary>
    /// Represents a fade command that controls sprite opacity
    /// </summary>
    public class FadeCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting opacity value (0.0 to 1.0)
        /// </summary>
        public float StartOpacity { get; set; }
        
        /// <summary>
        /// Ending opacity value (0.0 to 1.0)
        /// </summary>
        public float EndOpacity { get; set; }
        
        public FadeCommand()
        {
            CommandType = "F";
        }
        
        public override string ToString()
        {
            return $"Fade: {StartOpacity} -> {EndOpacity} ({StartTime}ms - {EndTime}ms)";
        }
    }
    
    /// <summary>
    /// Represents a move command that controls sprite position
    /// </summary>
    public class MoveCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting X position
        /// </summary>
        public float StartX { get; set; }
        
        /// <summary>
        /// Starting Y position
        /// </summary>
        public float StartY { get; set; }
        
        /// <summary>
        /// Ending X position
        /// </summary>
        public float EndX { get; set; }
        
        /// <summary>
        /// Ending Y position
        /// </summary>
        public float EndY { get; set; }
        
        public MoveCommand()
        {
            CommandType = "M";
        }
        
        public override string ToString()
        {
            return $"Move: ({StartX}, {StartY}) -> ({EndX}, {EndY}) ({StartTime}ms - {EndTime}ms)";
        }
    }
    
    /// <summary>
    /// Represents a scale command that controls sprite size
    /// </summary>
    public class ScaleCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting scale factor
        /// </summary>
        public float StartScale { get; set; }
        
        /// <summary>
        /// Ending scale factor
        /// </summary>
        public float EndScale { get; set; }
        
        public ScaleCommand()
        {
            CommandType = "S";
        }
        
        public override string ToString()
        {
            return $"Scale: {StartScale} -> {EndScale} ({StartTime}ms - {EndTime}ms)";
        }
    }
    
    /// <summary>
    /// Represents a vector scale command that controls sprite size independently on X and Y axes
    /// </summary>
    public class VectorScaleCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting X scale factor
        /// </summary>
        public float StartScaleX { get; set; }
        
        /// <summary>
        /// Starting Y scale factor
        /// </summary>
        public float StartScaleY { get; set; }
        
        /// <summary>
        /// Ending X scale factor
        /// </summary>
        public float EndScaleX { get; set; }
        
        /// <summary>
        /// Ending Y scale factor
        /// </summary>
        public float EndScaleY { get; set; }
        
        public VectorScaleCommand()
        {
            CommandType = "V";
        }
        
        public override string ToString()
        {
            return $"VectorScale: ({StartScaleX}, {StartScaleY}) -> ({EndScaleX}, {EndScaleY}) ({StartTime}ms - {EndTime}ms)";
        }
    }
    
    /// <summary>
    /// Represents a rotate command that controls sprite rotation
    /// </summary>
    public class RotateCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting rotation angle in radians
        /// </summary>
        public float StartAngle { get; set; }
        
        /// <summary>
        /// Ending rotation angle in radians
        /// </summary>
        public float EndAngle { get; set; }
        
        public RotateCommand()
        {
            CommandType = "R";
        }
        
        public override string ToString()
        {
            return $"Rotate: {StartAngle}rad -> {EndAngle}rad ({StartTime}ms - {EndTime}ms)";
        }
    }

    /// <summary>
    /// Represents a parameter command that applies special effects (H, V, A)
    /// Unlike other commands, parameters only apply while they are active
    /// </summary>
    public class ParameterCommand : StoryboardCommand
    {
        /// <summary>
        /// Parameter type: "H" (horizontal flip), "V" (vertical flip), "A" (additive blending)
        /// </summary>
        public string Parameter { get; set; } = string.Empty;

        public ParameterCommand()
        {
            CommandType = "P";
        }

        public override string ToString()
        {
            return $"Parameter: {Parameter} ({StartTime}ms - {EndTime}ms)";
        }
    }

    /// <summary>
    /// Represents a move X command that controls sprite X position only
    /// </summary>
    public class MoveXCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting X position
        /// </summary>
        public float StartX { get; set; }

        /// <summary>
        /// Ending X position
        /// </summary>
        public float EndX { get; set; }

        public MoveXCommand()
        {
            CommandType = "MX";
        }

        public override string ToString()
        {
            return $"MoveX: {StartX} -> {EndX} ({StartTime}ms - {EndTime}ms)";
        }
    }

    /// <summary>
    /// Represents a move Y command that controls sprite Y position only
    /// </summary>
    public class MoveYCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting Y position
        /// </summary>
        public float StartY { get; set; }

        /// <summary>
        /// Ending Y position
        /// </summary>
        public float EndY { get; set; }

        public MoveYCommand()
        {
            CommandType = "MY";
        }

        public override string ToString()
        {
            return $"MoveY: {StartY} -> {EndY} ({StartTime}ms - {EndTime}ms)";
        }
    }

    /// <summary>
    /// Represents a color command that controls sprite tinting
    /// </summary>
    public class ColorCommand : StoryboardCommand
    {
        /// <summary>
        /// Starting red component (0-255)
        /// </summary>
        public byte StartRed { get; set; }

        /// <summary>
        /// Starting green component (0-255)
        /// </summary>
        public byte StartGreen { get; set; }

        /// <summary>
        /// Starting blue component (0-255)
        /// </summary>
        public byte StartBlue { get; set; }

        /// <summary>
        /// Ending red component (0-255)
        /// </summary>
        public byte EndRed { get; set; }

        /// <summary>
        /// Ending green component (0-255)
        /// </summary>
        public byte EndGreen { get; set; }

        /// <summary>
        /// Ending blue component (0-255)
        /// </summary>
        public byte EndBlue { get; set; }

        public ColorCommand()
        {
            CommandType = "C";
        }

        public override string ToString()
        {
            return $"Color: ({StartRed},{StartGreen},{StartBlue}) -> ({EndRed},{EndGreen},{EndBlue}) ({StartTime}ms - {EndTime}ms)";
        }
    }

    /// <summary>
    /// Represents a loop command that repeats a set of commands
    /// </summary>
    public class LoopCommand : StoryboardCommand
    {
        /// <summary>
        /// Number of times the loop executes
        /// </summary>
        public int LoopCount { get; set; }

        /// <summary>
        /// Commands to be repeated within the loop
        /// </summary>
        public List<StoryboardCommand> LoopCommands { get; set; } = new List<StoryboardCommand>();

        public LoopCommand()
        {
            CommandType = "L";
        }

        /// <summary>
        /// Expands the loop into individual commands with absolute timestamps
        /// </summary>
        /// <returns>List of expanded commands</returns>
        public List<StoryboardCommand> ExpandLoop()
        {
            var expandedCommands = new List<StoryboardCommand>();

            // Calculate loop duration based on the longest command inside the loop
            var loopDuration = LoopCommands.Any() ? LoopCommands.Max(c => c.EndTime) : 0;

            for (int iteration = 0; iteration < LoopCount; iteration++)
            {
                var iterationOffset = StartTime + (iteration * loopDuration);

                foreach (var loopCommand in LoopCommands)
                {
                    var expandedCommand = cloneCommandWithOffset(loopCommand, iterationOffset);
                    expandedCommands.Add(expandedCommand);
                }
            }

            return expandedCommands;
        }

        private static StoryboardCommand cloneCommandWithOffset(StoryboardCommand original, int offset)
        {
            return original switch
            {
                FadeCommand fade => new FadeCommand
                {
                    Easing = fade.Easing,
                    StartTime = fade.StartTime + offset,
                    EndTime = fade.EndTime + offset,
                    StartOpacity = fade.StartOpacity,
                    EndOpacity = fade.EndOpacity
                },
                MoveCommand move => new MoveCommand
                {
                    Easing = move.Easing,
                    StartTime = move.StartTime + offset,
                    EndTime = move.EndTime + offset,
                    StartX = move.StartX,
                    StartY = move.StartY,
                    EndX = move.EndX,
                    EndY = move.EndY
                },
                ScaleCommand scale => new ScaleCommand
                {
                    Easing = scale.Easing,
                    StartTime = scale.StartTime + offset,
                    EndTime = scale.EndTime + offset,
                    StartScale = scale.StartScale,
                    EndScale = scale.EndScale
                },
                VectorScaleCommand vecScale => new VectorScaleCommand
                {
                    Easing = vecScale.Easing,
                    StartTime = vecScale.StartTime + offset,
                    EndTime = vecScale.EndTime + offset,
                    StartScaleX = vecScale.StartScaleX,
                    StartScaleY = vecScale.StartScaleY,
                    EndScaleX = vecScale.EndScaleX,
                    EndScaleY = vecScale.EndScaleY
                },
                RotateCommand rotate => new RotateCommand
                {
                    Easing = rotate.Easing,
                    StartTime = rotate.StartTime + offset,
                    EndTime = rotate.EndTime + offset,
                    StartAngle = rotate.StartAngle,
                    EndAngle = rotate.EndAngle
                },
                ColorCommand color => new ColorCommand
                {
                    Easing = color.Easing,
                    StartTime = color.StartTime + offset,
                    EndTime = color.EndTime + offset,
                    StartRed = color.StartRed,
                    StartGreen = color.StartGreen,
                    StartBlue = color.StartBlue,
                    EndRed = color.EndRed,
                    EndGreen = color.EndGreen,
                    EndBlue = color.EndBlue
                },
                ParameterCommand param => new ParameterCommand
                {
                    Easing = param.Easing,
                    StartTime = param.StartTime + offset,
                    EndTime = param.EndTime + offset,
                    Parameter = param.Parameter
                },
                MoveXCommand moveX => new MoveXCommand
                {
                    Easing = moveX.Easing,
                    StartTime = moveX.StartTime + offset,
                    EndTime = moveX.EndTime + offset,
                    StartX = moveX.StartX,
                    EndX = moveX.EndX
                },
                MoveYCommand moveY => new MoveYCommand
                {
                    Easing = moveY.Easing,
                    StartTime = moveY.StartTime + offset,
                    EndTime = moveY.EndTime + offset,
                    StartY = moveY.StartY,
                    EndY = moveY.EndY
                },
                _ => throw new NotSupportedException($"Command type {original.GetType().Name} not supported in loops")
            };
        }

        public override string ToString()
        {
            return $"Loop: {LoopCount} iterations, {LoopCommands.Count} commands ({StartTime}ms)";
        }
    }
}