using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses color commands from OSB file lines
    /// </summary>
    public static class ColorCommandParser
    {
        /// <summary>
        /// Attempts to parse a color command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'C')</param>
        /// <returns>A ColorCommand if parsing succeeds, null otherwise</returns>
        public static ColorCommand ParseColorCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - color commands should have at least 4 parts
            // Format: C,easing,starttime,endtime,r1,g1,b1,r2,g2,b2
            if (parts.Length < 4 || !parts[0].Equals("C", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var colorCommand = new ColorCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    colorCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    colorCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (use start time if not provided)
                if (parts.Length > 3 && int.TryParse(parts[3], out int endTime))
                {
                    colorCommand.EndTime = endTime;
                }
                else
                {
                    colorCommand.EndTime = startTime; // Instant command
                }
                
                // Parse start RGB (required)
                if (parts.Length > 4 && byte.TryParse(parts[4], out byte startRed))
                {
                    colorCommand.StartRed = startRed;
                }
                else
                {
                    colorCommand.StartRed = 255; // Default to white
                }
                
                if (parts.Length > 5 && byte.TryParse(parts[5], out byte startGreen))
                {
                    colorCommand.StartGreen = startGreen;
                }
                else
                {
                    colorCommand.StartGreen = 255; // Default to white
                }
                
                if (parts.Length > 6 && byte.TryParse(parts[6], out byte startBlue))
                {
                    colorCommand.StartBlue = startBlue;
                }
                else
                {
                    colorCommand.StartBlue = 255; // Default to white
                }
                
                // Parse end RGB (use start RGB if not provided)
                if (parts.Length > 7 && byte.TryParse(parts[7], out byte endRed))
                {
                    colorCommand.EndRed = endRed;
                }
                else
                {
                    colorCommand.EndRed = colorCommand.StartRed; // No color change
                }
                
                if (parts.Length > 8 && byte.TryParse(parts[8], out byte endGreen))
                {
                    colorCommand.EndGreen = endGreen;
                }
                else
                {
                    colorCommand.EndGreen = colorCommand.StartGreen; // No color change
                }
                
                if (parts.Length > 9 && byte.TryParse(parts[9], out byte endBlue))
                {
                    colorCommand.EndBlue = endBlue;
                }
                else
                {
                    colorCommand.EndBlue = colorCommand.StartBlue; // No color change
                }
                
                return colorCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a color command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'C' (case insensitive)</returns>
        public static bool IsColorCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("C,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("C", StringComparison.OrdinalIgnoreCase);
        }
    }
}