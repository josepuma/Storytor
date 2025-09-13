using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses move commands from OSB file lines
    /// </summary>
    public static class MoveCommandParser
    {
        /// <summary>
        /// Attempts to parse a move command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'M')</param>
        /// <returns>A MoveCommand if parsing succeeds, null otherwise</returns>
        public static MoveCommand ParseMoveCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - move commands should have at least 4 parts
            // Format: M,easing,starttime,endtime,startx,starty,endx,endy
            if (parts.Length < 4 || !parts[0].Equals("M", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var moveCommand = new MoveCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    moveCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    moveCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (use start time if not provided)
                if (parts.Length > 3 && int.TryParse(parts[3], out int endTime))
                {
                    moveCommand.EndTime = endTime;
                }
                else
                {
                    moveCommand.EndTime = startTime; // Instant command
                }
                
                // Parse start X position (required)
                if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startX))
                {
                    moveCommand.StartX = startX;
                }
                else
                {
                    return null; // Start X is required
                }
                
                // Parse start Y position (required)
                if (parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float startY))
                {
                    moveCommand.StartY = startY;
                }
                else
                {
                    return null; // Start Y is required
                }
                
                // Parse end X position (use start X if not provided)
                if (parts.Length > 6 && float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float endX))
                {
                    moveCommand.EndX = endX;
                }
                else
                {
                    moveCommand.EndX = moveCommand.StartX; // No movement on X
                }
                
                // Parse end Y position (use start Y if not provided)
                if (parts.Length > 7 && float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float endY))
                {
                    moveCommand.EndY = endY;
                }
                else
                {
                    moveCommand.EndY = moveCommand.StartY; // No movement on Y
                }
                
                return moveCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a move command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'M' (case insensitive)</returns>
        public static bool IsMoveCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("M,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("M", StringComparison.OrdinalIgnoreCase);
        }
    }
}