using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses move X commands from OSB file lines
    /// </summary>
    public static class MoveXCommandParser
    {
        /// <summary>
        /// Attempts to parse a move X command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'MX')</param>
        /// <returns>A MoveXCommand if parsing succeeds, null otherwise</returns>
        public static MoveXCommand ParseMoveXCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - move X commands should have at least 4 parts
            // Format: MX,easing,starttime,endtime,startx,endx
            if (parts.Length < 4 || !parts[0].Equals("MX", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var moveXCommand = new MoveXCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    moveXCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    moveXCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (handle empty values for persistent commands)
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && int.TryParse(parts[3], out int endTime))
                {
                    moveXCommand.EndTime = endTime;
                }
                else if (parts.Length > 3 && string.IsNullOrWhiteSpace(parts[3]))
                {
                    // Empty end time means persistent command - let renderer handle duration
                    moveXCommand.EndTime = int.MaxValue;
                }
                else
                {
                    moveXCommand.EndTime = startTime; // Instant command (fallback)
                }
                
                // Parse start X (required)
                if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startX))
                {
                    moveXCommand.StartX = startX;
                }
                else
                {
                    return null; // Start X is required
                }
                
                // Parse end X (use start X if not provided)
                if (parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float endX))
                {
                    moveXCommand.EndX = endX;
                }
                else
                {
                    moveXCommand.EndX = moveXCommand.StartX; // No X movement
                }
                
                // Fix persistent vs instantaneous logic
                if (moveXCommand.EndTime == int.MaxValue)
                {
                    // Empty endtime - check if X values are different
                    if (Math.Abs(moveXCommand.StartX - moveXCommand.EndX) > 0.001f)
                    {
                        // Different X values = instantaneous move, not persistent
                        moveXCommand.EndTime = moveXCommand.StartTime;
                    }
                    // If X values are the same, keep EndTime = int.MaxValue (persistent)
                }
                
                return moveXCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a move X command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'MX' (case insensitive)</returns>
        public static bool IsMoveXCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("MX,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("MX", StringComparison.OrdinalIgnoreCase);
        }
    }
}