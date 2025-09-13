using System;
using System.Collections.Generic;
using System.Globalization;

namespace storytor.Game.Storyboard.Models
{
    /// <summary>
    /// Represents a storyboard command that can be applied to a sprite
    /// </summary>
    public class StoryboardCommand
    {
        public string CommandType { get; set; } = string.Empty;
        public int Easing { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        
        // Single value commands (F, S, R, MX, MY)
        public double StartValue { get; set; }
        public double EndValue { get; set; }
        
        // Two value commands (M, V)
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        
        // Color commands (C)
        public double StartRed { get; set; }
        public double StartGreen { get; set; }
        public double StartBlue { get; set; }
        public double EndRed { get; set; }
        public double EndGreen { get; set; }
        public double EndBlue { get; set; }
        
        // Parameter commands (P)
        public string ParameterType { get; set; } = string.Empty;

        // Loop commands (L) - store original loop structure
        public bool IsLoop { get; set; } = false;
        public int LoopCount { get; set; } = 0;
        public List<StoryboardCommand> LoopCommands { get; set; } = new List<StoryboardCommand>();
        
        /// <summary>
        /// Creates a command from OSB line format: CommandType,Easing,StartTime,EndTime,Values...
        /// </summary>
        public static StoryboardCommand FromOsbLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            
            var values = line.Split(',');
            if (values.Length < 4) return null;
            
            var command = new StoryboardCommand
            {
                CommandType = values[0].Trim(),
                Easing = int.TryParse(values[1], out int easing) ? easing : 0
            };
            
            // Parse timing
            if (!double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double startTime))
                return null;
            command.StartTime = startTime;
            
            // Handle end time - if empty, use start time (instant command)
            if (string.IsNullOrWhiteSpace(values[3]))
            {
                command.EndTime = startTime;
            }
            else if (double.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double endTime))
            {
                command.EndTime = endTime;
            }
            else
            {
                command.EndTime = startTime;
            }
            
            // Parse values based on command type
            switch (command.CommandType.ToUpperInvariant())
            {
                case "F": // Fade
                    if (values.Length > 4) 
                    {
                        if (double.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double startValue))
                            command.StartValue = startValue;
                    }
                    if (values.Length > 5) 
                    {
                        if (double.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double endValue))
                            command.EndValue = endValue;
                        else 
                            command.EndValue = command.StartValue;
                    }
                    else command.EndValue = command.StartValue;
                    break;
                    
                case "S": // Scale
                case "R": // Rotate  
                case "MX": // MoveX
                case "MY": // MoveY
                    if (values.Length > 4) 
                    {
                        if (double.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double startValue))
                            command.StartValue = startValue;
                    }
                    if (values.Length > 5) 
                    {
                        if (double.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double endValue))
                            command.EndValue = endValue;
                        else 
                            command.EndValue = command.StartValue;
                    }
                    else command.EndValue = command.StartValue;
                    break;
                    
                case "M": // Move
                case "V": // Vector Scale
                    if (values.Length > 4) 
                    {
                        if (double.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double startX))
                            command.StartX = startX;
                    }
                    if (values.Length > 5) 
                    {
                        if (double.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double startY))
                            command.StartY = startY;
                    }
                    if (values.Length > 6) 
                    {
                        if (double.TryParse(values[6], NumberStyles.Float, CultureInfo.InvariantCulture, out double endX))
                            command.EndX = endX;
                        else 
                            command.EndX = command.StartX;
                    }
                    else command.EndX = command.StartX;
                    if (values.Length > 7) 
                    {
                        if (double.TryParse(values[7], NumberStyles.Float, CultureInfo.InvariantCulture, out double endY))
                            command.EndY = endY;
                        else 
                            command.EndY = command.StartY;
                    }
                    else command.EndY = command.StartY;
                    break;
                    
                case "C": // Color
                    if (values.Length > 4) 
                    {
                        if (double.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double startRed))
                            command.StartRed = startRed;
                    }
                    if (values.Length > 5) 
                    {
                        if (double.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double startGreen))
                            command.StartGreen = startGreen;
                    }
                    if (values.Length > 6) 
                    {
                        if (double.TryParse(values[6], NumberStyles.Float, CultureInfo.InvariantCulture, out double startBlue))
                            command.StartBlue = startBlue;
                    }
                    if (values.Length > 7) 
                    {
                        if (double.TryParse(values[7], NumberStyles.Float, CultureInfo.InvariantCulture, out double endRed))
                            command.EndRed = endRed;
                        else 
                            command.EndRed = command.StartRed;
                    }
                    else command.EndRed = command.StartRed;
                    if (values.Length > 8) 
                    {
                        if (double.TryParse(values[8], NumberStyles.Float, CultureInfo.InvariantCulture, out double endGreen))
                            command.EndGreen = endGreen;
                        else 
                            command.EndGreen = command.StartGreen;
                    }
                    else command.EndGreen = command.StartGreen;
                    if (values.Length > 9) 
                    {
                        if (double.TryParse(values[9], NumberStyles.Float, CultureInfo.InvariantCulture, out double endBlue))
                            command.EndBlue = endBlue;
                        else 
                            command.EndBlue = command.StartBlue;
                    }
                    else command.EndBlue = command.StartBlue;
                    break;
                    
                case "P": // Parameter
                    if (values.Length > 4) command.ParameterType = values[4].Trim();
                    break;
            }
            
            return command;
        }
        
        /// <summary>
        /// Gets the interpolated single value at a specific time (for F, S, R, MX, MY commands)
        /// </summary>
        public double GetValueAt(double time)
        {
            if (StartTime >= EndTime) return StartValue;
            
            double progress = Math.Clamp((time - StartTime) / (EndTime - StartTime), 0.0, 1.0);
            progress = ApplyEasing(progress, Easing);
            return Lerp(StartValue, EndValue, progress);
        }
        
        /// <summary>
        /// Gets the interpolated X,Y values at a specific time (for M, V commands)
        /// </summary>
        public (double x, double y) GetPositionAt(double time)
        {
            if (StartTime >= EndTime) return (StartX, StartY);
            
            double progress = Math.Clamp((time - StartTime) / (EndTime - StartTime), 0.0, 1.0);
            progress = ApplyEasing(progress, Easing);
            return (Lerp(StartX, EndX, progress), Lerp(StartY, EndY, progress));
        }
        
        /// <summary>
        /// Gets the interpolated RGB values at a specific time (for C commands)
        /// </summary>
        public (double red, double green, double blue) GetColorAt(double time)
        {
            if (StartTime >= EndTime) return (StartRed, StartGreen, StartBlue);
            
            double progress = Math.Clamp((time - StartTime) / (EndTime - StartTime), 0.0, 1.0);
            progress = ApplyEasing(progress, Easing);
            return (
                Lerp(StartRed, EndRed, progress),
                Lerp(StartGreen, EndGreen, progress), 
                Lerp(StartBlue, EndBlue, progress)
            );
        }
        
        private static double Lerp(double start, double end, double t) => start + (end - start) * t;
        
        private static double ApplyEasing(double t, int easingType)
        {
            return easingType switch
            {
                1 => 1 - Math.Pow(1 - t, 2), // Ease Out
                2 => Math.Pow(t, 2), // Ease In  
                3 => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2, // Ease In-Out
                _ => t // Linear (no easing)
            };
        }
        
        public override string ToString()
        {
            return CommandType switch
            {
                "F" => $"Fade: {StartValue} -> {EndValue} ({StartTime}ms - {EndTime}ms)",
                "M" => $"Move: ({StartX}, {StartY}) -> ({EndX}, {EndY}) ({StartTime}ms - {EndTime}ms)",
                "S" => $"Scale: {StartValue} -> {EndValue} ({StartTime}ms - {EndTime}ms)",
                "V" => $"VectorScale: ({StartX}, {StartY}) -> ({EndX}, {EndY}) ({StartTime}ms - {EndTime}ms)",
                "R" => $"Rotate: {StartValue} -> {EndValue} ({StartTime}ms - {EndTime}ms)",
                "C" => $"Color: ({StartRed},{StartGreen},{StartBlue}) -> ({EndRed},{EndGreen},{EndBlue}) ({StartTime}ms - {EndTime}ms)",
                "P" => $"Parameter: {ParameterType} ({StartTime}ms - {EndTime}ms)",
                "MX" => $"MoveX: {StartValue} -> {EndValue} ({StartTime}ms - {EndTime}ms)",
                "MY" => $"MoveY: {StartValue} -> {EndValue} ({StartTime}ms - {EndTime}ms)",
                _ => $"{CommandType}: ({StartTime}ms - {EndTime}ms)"
            };
        }
    }
}