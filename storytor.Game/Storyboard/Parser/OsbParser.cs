using System;
using System.Globalization;
using System.Threading.Tasks;
using osu.Framework.Graphics;
using storytor.Game.Storyboard.Models;
using storytor.Game.Storyboard.Parser.CommandParsers;
using storytor.Game.Storyboard.Reader;

namespace storytor.Game.Storyboard.Parser
{
    /// <summary>
    /// Main parser for OSB storyboard files
    /// </summary>
    public class OsbParser
    {
        /// <summary>
        /// Parses an OSB file and returns a complete Storyboard object
        /// </summary>
        /// <param name="filePath">Path to the .osb file</param>
        /// <returns>A parsed Storyboard object</returns>
        public static async Task<StoryboardData> ParseAsync(string filePath)
        {
            string[] lines = await OsbFileReader.ReadOsbFileAsync(filePath);
            return parse(lines, filePath);
        }
        
        /// <summary>
        /// Parses an OSB file synchronously and returns a complete Storyboard object
        /// </summary>
        /// <param name="filePath">Path to the .osb file</param>
        /// <returns>A parsed Storyboard object</returns>
        public static StoryboardData ParseFile(string filePath)
        {
            string[] lines = OsbFileReader.ReadOsbFile(filePath);
            return parse(lines, filePath);
        }
        
        /// <summary>
        /// Parses OSB content from an array of lines
        /// </summary>
        /// <param name="lines">Lines from the OSB file</param>
        /// <param name="filePath">Original file path for reference</param>
        /// <returns>A parsed Storyboard object</returns>
        private static StoryboardData parse(string[] lines, string filePath)
        {
            var storyboard = new StoryboardData { FilePath = filePath };
            StoryboardSprite currentSprite = null;
            LoopCommand currentLoop = null;
            int spriteIdCounter = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmedLine = lines[i].Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;
                
                // Check if this is a sprite definition
                if (isSpriteDefinition(trimmedLine))
                {
                    // Save previous sprite if it exists
                    if (currentSprite != null)
                    {
                        storyboard.Sprites.Add(currentSprite);
                    }
                    
                    // Parse new sprite
                    currentSprite = parseSprite(trimmedLine);
                    if (currentSprite != null)
                    {
                        currentSprite.Id = spriteIdCounter++;
                    }
                    currentLoop = null; // Reset loop when starting new sprite
                }
                else if (currentSprite != null)
                {
                    // Check if this is a loop command
                    if (LoopCommandParser.IsLoopCommand(trimmedLine))
                    {
                        if (currentSprite?.ImagePath?.Contains("background.png") == true)
                            Console.WriteLine($"🔄 Parsing Loop: {trimmedLine}");
                        // Parse loop command
                        currentLoop = LoopCommandParser.ParseLoopCommand(trimmedLine);
                        if (currentLoop != null)
                        {
                            if (currentSprite?.ImagePath?.Contains("background.png") == true)
                                Console.WriteLine($"✅ Loop parsed: StartTime={currentLoop.StartTime}, LoopCount={currentLoop.LoopCount}");
                            currentSprite.Commands.Add(currentLoop);
                        }
                    }
                    else if (currentLoop != null && lines[i].StartsWith("  "))
                    {
                        if (currentSprite?.ImagePath?.Contains("background.png") == true)
                            Console.WriteLine($"📝 Loop nested command: {trimmedLine}");
                        // We're inside a loop and the line is indented, parse nested commands
                        var command = parseCommand(trimmedLine);
                        if (command != null)
                        {
                            if (currentSprite?.ImagePath?.Contains("background.png") == true)
                                Console.WriteLine($"✅ Added to loop: {command.GetType().Name}, StartTime={command.StartTime}, EndTime={command.EndTime}");
                            currentLoop.LoopCommands.Add(command);
                            
                            // Update loop end time based on nested commands
                            var commandEndTime = command.EndTime > 0 ? command.EndTime : command.StartTime;
                            if (commandEndTime > currentLoop.EndTime - currentLoop.StartTime)
                            {
                                currentLoop.EndTime = currentLoop.StartTime + commandEndTime;
                            }
                        }
                    }
                    else
                    {
                        // Not indented or not in a loop - this closes any current loop
                        if (currentLoop != null && currentSprite?.ImagePath?.Contains("background.png") == true)
                            Console.WriteLine($"🔚 Closing loop due to non-indented line: {trimmedLine}");
                        currentLoop = null;
                        
                        // Regular command for the current sprite
                        var command = parseCommand(trimmedLine);
                        if (command != null)
                        {
                            if (currentSprite?.ImagePath?.Contains("background.png") == true)
                                Console.WriteLine($"➕ Regular command: {command.GetType().Name}, StartTime={command.StartTime}, EndTime={command.EndTime}");
                            currentSprite.Commands.Add(command);
                        }
                    }
                }
            }
            
            // Don't forget the last sprite
            if (currentSprite != null)
            {
                storyboard.Sprites.Add(currentSprite);
            }
            
            return storyboard;
        }
        
        /// <summary>
        /// Checks if a line represents a sprite definition
        /// </summary>
        private static bool isSpriteDefinition(string line)
        {
            return line.StartsWith("Sprite,", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Parses a sprite definition line
        /// </summary>
        private static StoryboardSprite parseSprite(string line)
        {
            // Format: Sprite,layer,origin,filepath,x,y
            string[] parts = line.Split(',');
            
            if (parts.Length < 6)
                return null;
            
            try
            {
                var sprite = new StoryboardSprite
                {
                    Layer = parts[1].Trim(),
                    Origin = parseOrigin(parts[2].Trim()),
                    ImagePath = parts[3].Trim().Trim('"'), // Remove quotes if present
                };
                
                // Parse X and Y coordinates
                if (float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                {
                    sprite.X = x;
                }
                
                if (float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    sprite.Y = y;
                }
                
                return sprite;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        /// <summary>
        /// Parses the origin string to an Anchor enum
        /// </summary>
        private static Anchor parseOrigin(string origin)
        {
            return origin.ToLowerInvariant() switch
            {
                "topleft" => Anchor.TopLeft,
                "topcentre" or "topcenter" => Anchor.TopCentre,
                "topright" => Anchor.TopRight,
                "centreleft" or "centerleft" => Anchor.CentreLeft,
                "centre" or "center" => Anchor.Centre,
                "centreright" or "centerright" => Anchor.CentreRight,
                "bottomleft" => Anchor.BottomLeft,
                "bottomcentre" or "bottomcenter" => Anchor.BottomCentre,
                "bottomright" => Anchor.BottomRight,
                _ => Anchor.Centre // Default fallback
            };
        }
        
        /// <summary>
        /// Attempts to parse a command from a line
        /// </summary>
        private static StoryboardCommand parseCommand(string line)
        {
            string trimmedLine = line.Trim();
            
            // Try all command parsers
            if (FadeCommandParser.IsFadeCommand(trimmedLine))
            {
                return FadeCommandParser.ParseFadeCommand(trimmedLine);
            }
            
            if (CommandParsers.MoveCommandParser.IsMoveCommand(trimmedLine))
            {
                return CommandParsers.MoveCommandParser.ParseMoveCommand(trimmedLine);
            }
            
            if (ScaleCommandParser.IsScaleCommand(trimmedLine))
            {
                return ScaleCommandParser.ParseScaleCommand(trimmedLine);
            }
            
            if (VectorScaleCommandParser.IsVectorScaleCommand(trimmedLine))
            {
                return VectorScaleCommandParser.ParseVectorScaleCommand(trimmedLine);
            }
            
            if (RotateCommandParser.IsRotateCommand(trimmedLine))
            {
                return RotateCommandParser.ParseRotateCommand(trimmedLine);
            }
            
            if (ParameterCommandParser.IsParameterCommand(trimmedLine))
            {
                return ParameterCommandParser.ParseParameterCommand(trimmedLine);
            }
            
            if (MoveXCommandParser.IsMoveXCommand(trimmedLine))
            {
                return MoveXCommandParser.ParseMoveXCommand(trimmedLine);
            }
            
            if (MoveYCommandParser.IsMoveYCommand(trimmedLine))
            {
                return MoveYCommandParser.ParseMoveYCommand(trimmedLine);
            }
            
            if (ColorCommandParser.IsColorCommand(trimmedLine))
            {
                return ColorCommandParser.ParseColorCommand(trimmedLine);
            }
            
            // Unknown command type
            return null;
        }
    }
}