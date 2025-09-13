using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses fade commands from OSB file lines
    /// </summary>
    public static class FadeCommandParser
    {
        /// <summary>
        /// Attempts to parse a fade command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'F')</param>
        /// <returns>A FadeCommand if parsing succeeds, null otherwise</returns>
        public static FadeCommand ParseFadeCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - fade commands should have at least 4 parts
            // Format: F,easing,starttime,endtime,startopacity,endopacity
            if (parts.Length < 4 || !parts[0].Equals("F", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var fadeCommand = new FadeCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    fadeCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    fadeCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (handle empty values for persistent commands)
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && int.TryParse(parts[3], out int endTime))
                {
                    fadeCommand.EndTime = endTime;
                }
                else if (parts.Length > 3 && string.IsNullOrWhiteSpace(parts[3]))
                {
                    // Empty end time means persistent command - let renderer handle duration
                    fadeCommand.EndTime = int.MaxValue;
                }
                else
                {
                    fadeCommand.EndTime = startTime; // Instant command (fallback)
                }
                
                // Parse start opacity (required)
                if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startOpacity))
                {
                    fadeCommand.StartOpacity = Math.Clamp(startOpacity, 0f, 1f);
                }
                else
                {
                    return null; // Start opacity is required
                }
                
                // Parse end opacity (use start opacity if not provided)
                if (parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float endOpacity))
                {
                    fadeCommand.EndOpacity = Math.Clamp(endOpacity, 0f, 1f);
                }
                else
                {
                    fadeCommand.EndOpacity = fadeCommand.StartOpacity; // No change
                }
                
                // Fix persistent vs instantaneous logic
                if (fadeCommand.EndTime == int.MaxValue)
                {
                    // Empty endtime - check if values are different
                    if (Math.Abs(fadeCommand.StartOpacity - fadeCommand.EndOpacity) > 0.001f)
                    {
                        // Different values = instantaneous change, not persistent
                        fadeCommand.EndTime = fadeCommand.StartTime;
                    }
                    // If values are the same, keep EndTime = int.MaxValue (persistent)
                }
                
                return fadeCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a fade command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'F' (case insensitive)</returns>
        public static bool IsFadeCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("F,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("F", StringComparison.OrdinalIgnoreCase);
        }
    }
}