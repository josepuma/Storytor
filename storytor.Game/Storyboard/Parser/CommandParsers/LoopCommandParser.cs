using System;
using System.Globalization;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses loop commands from OSB file lines
    /// </summary>
    public static class LoopCommandParser
    {
        /// <summary>
        /// Attempts to parse a loop command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with '_L')</param>
        /// <returns>A LoopCommand if parsing succeeds, null otherwise</returns>
        public static LoopCommand ParseLoopCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - loop commands should have exactly 3 parts
            // Format: _L,starttime,loopcount
            if (parts.Length != 3 || !parts[0].Equals("L", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var loopCommand = new LoopCommand();
                
                // Parse start time (required)
                if (int.TryParse(parts[1], out int startTime))
                {
                    loopCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse loop count (required)
                if (int.TryParse(parts[2], out int loopCount))
                {
                    loopCommand.LoopCount = loopCount;
                }
                else
                {
                    return null; // Loop count is required
                }
                
                // EndTime will be calculated after all nested commands are parsed
                loopCommand.EndTime = startTime;
                
                return loopCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a loop command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with '_L' (case insensitive)</returns>
        public static bool IsLoopCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("L,", StringComparison.OrdinalIgnoreCase);
        }
    }
}