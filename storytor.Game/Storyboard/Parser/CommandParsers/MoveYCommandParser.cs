using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses move Y commands from OSB file lines
    /// </summary>
    public static class MoveYCommandParser
    {
        /// <summary>
        /// Attempts to parse a move Y command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'MY')</param>
        /// <returns>A MoveYCommand if parsing succeeds, null otherwise</returns>
        public static MoveYCommand ParseMoveYCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - move Y commands should have at least 4 parts
            // Format: MY,easing,starttime,endtime,starty,endy
            if (parts.Length < 4 || !parts[0].Equals("MY", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var moveYCommand = new MoveYCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    moveYCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    moveYCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (handle empty values for persistent commands)
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && int.TryParse(parts[3], out int endTime))
                {
                    moveYCommand.EndTime = endTime;
                }
                else if (parts.Length > 3 && string.IsNullOrWhiteSpace(parts[3]))
                {
                    // Empty end time means persistent command - let renderer handle duration
                    moveYCommand.EndTime = int.MaxValue;
                }
                else
                {
                    moveYCommand.EndTime = startTime; // Instant command (fallback)
                }
                
                // Parse start Y (required)
                if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startY))
                {
                    moveYCommand.StartY = startY;
                }
                else
                {
                    return null; // Start Y is required
                }
                
                // Parse end Y (use start Y if not provided)
                if (parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float endY))
                {
                    moveYCommand.EndY = endY;
                }
                else
                {
                    moveYCommand.EndY = moveYCommand.StartY; // No Y movement
                }
                
                // Fix persistent vs instantaneous logic
                if (moveYCommand.EndTime == int.MaxValue)
                {
                    // Empty endtime - check if Y values are different
                    if (Math.Abs(moveYCommand.StartY - moveYCommand.EndY) > 0.001f)
                    {
                        // Different Y values = instantaneous move, not persistent
                        moveYCommand.EndTime = moveYCommand.StartTime;
                    }
                    // If Y values are the same, keep EndTime = int.MaxValue (persistent)
                }
                
                return moveYCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a move Y command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'MY' (case insensitive)</returns>
        public static bool IsMoveYCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("MY,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("MY", StringComparison.OrdinalIgnoreCase);
        }
    }
}