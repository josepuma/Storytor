using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses scale commands from OSB file lines
    /// </summary>
    public static class ScaleCommandParser
    {
        /// <summary>
        /// Attempts to parse a scale command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'S')</param>
        /// <returns>A ScaleCommand if parsing succeeds, null otherwise</returns>
        public static ScaleCommand ParseScaleCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - scale commands should have at least 4 parts
            // Format: S,easing,starttime,endtime,startscale,endscale
            if (parts.Length < 4 || !parts[0].Equals("S", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var scaleCommand = new ScaleCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    scaleCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    scaleCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (handle empty values for persistent commands)
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && int.TryParse(parts[3], out int endTime))
                {
                    scaleCommand.EndTime = endTime;
                }
                else if (parts.Length > 3 && string.IsNullOrWhiteSpace(parts[3]))
                {
                    // Empty end time means persistent command - let renderer handle duration
                    scaleCommand.EndTime = int.MaxValue;
                }
                else
                {
                    scaleCommand.EndTime = startTime; // Instant command (fallback)
                }
                
                // Parse start scale (required)
                if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startScale))
                {
                    scaleCommand.StartScale = startScale;
                }
                else
                {
                    return null; // Start scale is required
                }
                
                // Parse end scale (use start scale if not provided)
                if (parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float endScale))
                {
                    scaleCommand.EndScale = endScale;
                }
                else
                {
                    scaleCommand.EndScale = scaleCommand.StartScale; // No scaling change
                }
                
                // Fix persistent vs instantaneous logic
                if (scaleCommand.EndTime == int.MaxValue)
                {
                    // Empty endtime - check if scale values are different
                    if (Math.Abs(scaleCommand.StartScale - scaleCommand.EndScale) > 0.001f)
                    {
                        // Different scales = instantaneous scale, not persistent
                        scaleCommand.EndTime = scaleCommand.StartTime;
                    }
                    // If scales are the same, keep EndTime = int.MaxValue (persistent)
                }
                
                return scaleCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a scale command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'S' (case insensitive)</returns>
        public static bool IsScaleCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("S,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("S", StringComparison.OrdinalIgnoreCase);
        }
    }
}