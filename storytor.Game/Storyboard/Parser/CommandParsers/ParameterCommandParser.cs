using System;
using storytor.Game.Storyboard.Models;

namespace storytor.Game.Storyboard.Parser.CommandParsers
{
    /// <summary>
    /// Parses parameter commands from OSB file lines
    /// </summary>
    public static class ParameterCommandParser
    {
        /// <summary>
        /// Attempts to parse a parameter command from a line of text
        /// </summary>
        /// <param name="line">The line to parse (should start with 'P')</param>
        /// <returns>A ParameterCommand if parsing succeeds, null otherwise</returns>
        public static ParameterCommand ParseParameterCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Remove leading/trailing whitespace and split by comma
            string trimmedLine = line.Trim();
            string[] parts = trimmedLine.Split(',');
            
            // Basic validation - parameter commands should have at least 5 parts
            // Format: P,easing,starttime,endtime,parameter
            if (parts.Length < 5 || !parts[0].Equals("P", StringComparison.OrdinalIgnoreCase))
                return null;
            
            try
            {
                var parameterCommand = new ParameterCommand();
                
                // Parse easing (default to 0 if not provided or invalid)
                if (parts.Length > 1 && int.TryParse(parts[1], out int easing))
                {
                    parameterCommand.Easing = easing;
                }
                
                // Parse start time (required)
                if (parts.Length > 2 && int.TryParse(parts[2], out int startTime))
                {
                    parameterCommand.StartTime = startTime;
                }
                else
                {
                    return null; // Start time is required
                }
                
                // Parse end time (handle empty values for persistent parameters)
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && int.TryParse(parts[3], out int endTime))
                {
                    parameterCommand.EndTime = endTime;
                }
                else if (parts.Length > 3 && string.IsNullOrWhiteSpace(parts[3]))
                {
                    // Empty end time means persistent parameter - let renderer handle duration
                    parameterCommand.EndTime = int.MaxValue;
                }
                else
                {
                    parameterCommand.EndTime = startTime; // Instant command (fallback)
                }
                
                // Parse parameter (required)
                if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]))
                {
                    var parameter = parts[4].Trim().ToUpperInvariant();
                    // Validate parameter type
                    if (parameter == "H" || parameter == "V" || parameter == "A")
                    {
                        parameterCommand.Parameter = parameter;
                    }
                    else
                    {
                        return null; // Invalid parameter
                    }
                }
                else
                {
                    return null; // Parameter is required
                }
                
                return parameterCommand;
            }
            catch (Exception)
            {
                // If any parsing fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a line represents a parameter command
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if the line starts with 'P' (case insensitive)</returns>
        public static bool IsParameterCommand(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
                
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("P,", StringComparison.OrdinalIgnoreCase) ||
                   trimmedLine.Equals("P", StringComparison.OrdinalIgnoreCase);
        }
    }
}