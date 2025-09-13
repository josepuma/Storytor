using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses rotate commands from OSB file lines
    /// </summary>
    public static class RotateCommandParser
    {
        /// <summary>
        /// Attempts to parse a rotate command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'R')</param>
        /// <returns>A RotateCommand if parsing succeeds, null otherwise</returns>
        public static RotateCommand ParseRotateCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - rotate commands should have at least 4 parts
            // Format: R,easing,starttime,endtime,startangle,endangle
            if (parts.Length < 4 || !parts[0].Equals("R", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var rotateCommand = new RotateCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    rotateCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    rotateCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (use start time if not provided)
                if (parts.Length > 3 && int.TryParse(parts[3], out int endTime))
                {
                    rotateCommand.EndTime = endTime;
                }
                else
                {
                    rotateCommand.EndTime = startTime; // Instant command
                }
                
                // Parse start angle (required)
                if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startAngle))
                {
                    rotateCommand.StartAngle = startAngle;
                }
                else
                {
                    return null; // Start angle is required
                }
                
                // Parse end angle (use start angle if not provided)
                if (parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float endAngle))
                {
                    rotateCommand.EndAngle = endAngle;
                }
                else
                {
                    rotateCommand.EndAngle = rotateCommand.StartAngle; // No rotation change
                }
                
                return rotateCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a rotate command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'R' (case insensitive)</returns>
        public static bool IsRotateCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("R,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("R", StringComparison.OrdinalIgnoreCase);
        }
    }
}