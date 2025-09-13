using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using osu.Framework.Graphics;
using storytor.Game.Storyboard.Models;
using storytor.Game.Storyboard.Reader;

namespace storytor.Game.Storyboard.Parser
{
    /// <summary>
    /// OSB parser for storyboard files
    /// </summary>
    public static class OsbParser
    {
        /// <summary>
        /// Parses an OSB file asynchronously and returns a complete Storyboard object
        /// </summary>
        public static async Task<StoryboardData> ParseAsync(string filePath)
        {
            string[] lines = await OsbFileReader.ReadOsbFileAsync(filePath);
            return Parse(lines, filePath);
        }

        /// <summary>
        /// Parses an OSB file synchronously and returns a complete Storyboard object
        /// </summary>
        public static StoryboardData ParseFile(string filePath)
        {
            string[] lines = OsbFileReader.ReadOsbFile(filePath);
            return Parse(lines, filePath);
        }

        /// <summary>
        /// Parses OSB content from lines
        /// </summary>
        public static StoryboardData Parse(string[] lines, string filePath = "")
        {
            var storyboard = new StoryboardData { FilePath = filePath };
            StoryboardSprite currentSprite = null;
            var inCommandGroup = false;
            var currentGroupCommands = new List<StoryboardCommand>();
            double? currentLoopStartTime = null;
            int? currentLoopCount = null;
            int spriteIdCounter = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//"))
                    continue;

                var depth = getIndentationDepth(line);
                var trimmedLine = line.Trim();
                var values = trimmedLine.Split(',');

                // Close command group if we're no longer indented
                if (inCommandGroup && depth < 2)
                {
                    if (currentSprite != null)
                    {
                        // If it was a loop, create a loop command structure
                        if (currentLoopStartTime.HasValue && currentLoopCount.HasValue)
                        {
                            var loopCommand = new StoryboardCommand
                            {
                                CommandType = "L",
                                IsLoop = true,
                                StartTime = currentLoopStartTime.Value,
                                EndTime = currentLoopStartTime.Value, // Will be calculated during rendering
                                LoopCount = currentLoopCount.Value,
                                LoopCommands = new List<StoryboardCommand>(currentGroupCommands)
                            };
                            currentSprite.Commands.Add(loopCommand);
                        }
                        else
                        {
                            // Regular group commands (triggers, etc.)
                            foreach (var groupCommand in currentGroupCommands)
                            {
                                currentSprite.Commands.Add(groupCommand);
                            }
                        }
                    }
                    inCommandGroup = false;
                    currentGroupCommands.Clear();
                    currentLoopStartTime = null;
                    currentLoopCount = null;
                }

                switch (values[0])
                {
                    case "Sprite":
                        // Save previous sprite if it exists
                        if (currentSprite != null)
                        {
                            storyboard.Sprites.Add(currentSprite);
                        }

                        currentSprite = parseSprite(values);
                        if (currentSprite != null)
                        {
                            currentSprite.Id = spriteIdCounter++;
                        }
                        break;

                    case "Animation":
                        // Save previous sprite if it exists
                        if (currentSprite != null)
                        {
                            storyboard.Sprites.Add(currentSprite);
                        }

                        currentSprite = parseAnimation(values);
                        if (currentSprite != null)
                        {
                            currentSprite.Id = spriteIdCounter++;
                        }
                        break;

                    case "L": // Loop
                        currentLoopStartTime = double.Parse(values[1], CultureInfo.InvariantCulture);
                        currentLoopCount = int.Parse(values[2]);
                        inCommandGroup = true;
                        currentGroupCommands.Clear();
                        break;

                    case "T": // Trigger (similar to loop handling)
                        inCommandGroup = true;
                        currentGroupCommands.Clear();
                        break;

                    default:
                        // Regular command
                        var command = StoryboardCommand.FromOsbLine(trimmedLine);
                        if (command != null && currentSprite != null)
                        {
                            if (inCommandGroup)
                            {
                                currentGroupCommands.Add(command);
                            }
                            else
                            {
                                currentSprite.Commands.Add(command);
                            }
                        }
                        break;
                }
            }

            // Don't forget the last sprite and any remaining group commands
            if (inCommandGroup && currentSprite != null)
            {
                if (currentLoopStartTime.HasValue && currentLoopCount.HasValue)
                {
                    var loopCommand = new StoryboardCommand
                    {
                        CommandType = "L",
                        IsLoop = true,
                        StartTime = currentLoopStartTime.Value,
                        EndTime = currentLoopStartTime.Value,
                        LoopCount = currentLoopCount.Value,
                        LoopCommands = new List<StoryboardCommand>(currentGroupCommands)
                    };
                    currentSprite.Commands.Add(loopCommand);
                }
                else
                {
                    foreach (var groupCommand in currentGroupCommands)
                    {
                        currentSprite.Commands.Add(groupCommand);
                    }
                }
            }

            if (currentSprite != null)
            {
                storyboard.Sprites.Add(currentSprite);
            }

            return storyboard;
        }

        private static StoryboardSprite parseSprite(string[] values)
        {
            if (values.Length < 6) return null;

            try
            {
                return new StoryboardSprite
                {
                    Layer = values[1].Trim(),
                    Origin = parseOrigin(values[2].Trim()),
                    ImagePath = removeQuotes(values[3].Trim()),
                    X = float.Parse(values[4], CultureInfo.InvariantCulture),
                    Y = float.Parse(values[5], CultureInfo.InvariantCulture)
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static StoryboardSprite parseAnimation(string[] values)
        {
            if (values.Length < 9) return null;

            try
            {
                return new StoryboardSprite
                {
                    Layer = values[1].Trim(),
                    Origin = parseOrigin(values[2].Trim()),
                    ImagePath = removeQuotes(values[3].Trim()),
                    X = float.Parse(values[4], CultureInfo.InvariantCulture),
                    Y = float.Parse(values[5], CultureInfo.InvariantCulture),
                    // Animation-specific properties could be added to StoryboardSprite if needed
                    // FrameCount = int.Parse(values[6]),
                    // FrameDelay = double.Parse(values[7], CultureInfo.InvariantCulture),
                    // LoopType = values[8]
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

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
                _ => Anchor.Centre
            };
        }

        private static string removeQuotes(string path)
        {
            return path.StartsWith("\"") && path.EndsWith("\"")
                ? path[1..^1]
                : path;
        }

        private static int getIndentationDepth(string line)
        {
            int depth = 0;
            while (depth < line.Length && line[depth] == ' ')
                depth++;
            return depth;
        }

    }
}
