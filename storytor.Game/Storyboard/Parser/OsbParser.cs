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
            return Parse(lines, filePath);
        }
        
        /// <summary>
        /// Parses an OSB file synchronously and returns a complete Storyboard object
        /// </summary>
        /// <param name="filePath">Path to the .osb file</param>
        /// <returns>A parsed Storyboard object</returns>
        public static StoryboardData ParseFile(string filePath)
        {
            string[] lines = OsbFileReader.ReadOsbFile(filePath);
            return Parse(lines, filePath);
        }
        
        /// <summary>
        /// Parses OSB content from an array of lines
        /// </summary>
        /// <param name="lines">Lines from the OSB file</param>
        /// <param name="filePath">Original file path for reference</param>
        /// <returns>A parsed Storyboard object</returns>
        private static StoryboardData Parse(string[] lines, string filePath)
        {
            var storyboard = new StoryboardData { FilePath = filePath };
            StoryboardSprite currentSprite = null;
            int spriteIdCounter = 0;
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;
                
                // Check if this is a sprite definition
                if (IsSpriteDefinition(trimmedLine))
                {
                    // Save previous sprite if it exists
                    if (currentSprite != null)
                    {
                        storyboard.Sprites.Add(currentSprite);
                    }
                    
                    // Parse new sprite
                    currentSprite = ParseSprite(trimmedLine);
                    if (currentSprite != null)
                    {
                        currentSprite.Id = spriteIdCounter++;
                    }
                }
                else if (currentSprite != null)
                {
                    // Try to parse as a command for the current sprite
                    var command = ParseCommand(trimmedLine);
                    if (command != null)
                    {
                        currentSprite.Commands.Add(command);
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
        private static bool IsSpriteDefinition(string line)
        {
            return line.StartsWith("Sprite,", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Parses a sprite definition line
        /// </summary>
        private static StoryboardSprite ParseSprite(string line)
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
                    Origin = ParseOrigin(parts[2].Trim()),
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
        private static Anchor ParseOrigin(string origin)
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
        private static StoryboardCommand ParseCommand(string line)
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