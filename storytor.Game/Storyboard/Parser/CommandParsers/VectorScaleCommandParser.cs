using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses vector scale commands from OSB file lines
    /// </summary>
    public static class VectorScaleCommandParser
    {
        /// <summary>
        /// Attempts to parse a vector scale command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'V')</param>
        /// <returns>A VectorScaleCommand if parsing succeeds, null otherwise</returns>
        public static VectorScaleCommand ParseVectorScaleCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - vector scale commands should have at least 4 parts
            // Format: V,easing,starttime,endtime,startscalex,startscaley,endscalex,endscaley
            if (parts.Length < 4 || !parts[0].Equals("V", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var vectorScaleCommand = new VectorScaleCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    vectorScaleCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    vectorScaleCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (handle empty values for persistent commands)
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && int.TryParse(parts[3], out int endTime))
                {
                    vectorScaleCommand.EndTime = endTime;
                }
                else if (parts.Length > 3 && string.IsNullOrWhiteSpace(parts[3]))
                {
                    // Empty end time means persistent command - let renderer handle duration
                    vectorScaleCommand.EndTime = int.MaxValue;
                }
                else
                {
                    vectorScaleCommand.EndTime = startTime; // Instant command (fallback)
                }
                
                // Parse start scale X (required)
                if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startScaleX))
                {
                    vectorScaleCommand.StartScaleX = startScaleX;
                }
                else
                {
                    return null; // Start scale X is required
                }
                
                // Parse start scale Y (required)
                if (parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float startScaleY))
                {
                    vectorScaleCommand.StartScaleY = startScaleY;
                }
                else
                {
                    return null; // Start scale Y is required
                }
                
                // Parse end scale X (use start scale X if not provided)
                if (parts.Length > 6 && float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float endScaleX))
                {
                    vectorScaleCommand.EndScaleX = endScaleX;
                }
                else
                {
                    vectorScaleCommand.EndScaleX = vectorScaleCommand.StartScaleX; // No X scaling change
                }
                
                // Parse end scale Y (use start scale Y if not provided)
                if (parts.Length > 7 && float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float endScaleY))
                {
                    vectorScaleCommand.EndScaleY = endScaleY;
                }
                else
                {
                    vectorScaleCommand.EndScaleY = vectorScaleCommand.StartScaleY; // No Y scaling change
                }
                
                // Fix persistent vs instantaneous logic
                if (vectorScaleCommand.EndTime == int.MaxValue)
                {
                    // Empty endtime - check if scale values are different
                    if (Math.Abs(vectorScaleCommand.StartScaleX - vectorScaleCommand.EndScaleX) > 0.001f ||
                        Math.Abs(vectorScaleCommand.StartScaleY - vectorScaleCommand.EndScaleY) > 0.001f)
                    {
                        // Different scales = instantaneous scale change, not persistent
                        vectorScaleCommand.EndTime = vectorScaleCommand.StartTime;
                    }
                    // If scales are the same, keep EndTime = int.MaxValue (persistent)
                }
                
                return vectorScaleCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a vector scale command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'V' (case insensitive)</returns>
        public static bool IsVectorScaleCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("V,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("V", StringComparison.OrdinalIgnoreCase);
        }
    }
}