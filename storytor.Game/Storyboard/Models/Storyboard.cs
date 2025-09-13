using System.Collections.Generic;
using System.Linq;

namespace storytor.Game.Storyboard.Models
{
    /// <summary>
    /// Represents a complete storyboard with all its sprites and metadata
    /// </summary>
    public class StoryboardData
    {
        /// <summary>
        /// The source file path of this storyboard
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// List of all sprites in the storyboard
        /// </summary>
        public List<StoryboardSprite> Sprites { get; set; } = new List<StoryboardSprite>();
        
        /// <summary>
        /// Gets the total number of commands across all sprites
        /// </summary>
        public int TotalCommands => Sprites.Sum(sprite => sprite.Commands.Count);
        
        /// <summary>
        /// Gets all unique layers used in this storyboard
        /// </summary>
        public IEnumerable<string> UsedLayers => Sprites.Select(s => s.Layer).Distinct();
        
        /// <summary>
        /// Gets sprites filtered by layer
        /// </summary>
        /// <param name="layer">The layer name to filter by</param>
        /// <returns>Sprites in the specified layer</returns>
        public IEnumerable<StoryboardSprite> GetSpritesByLayer(string layer)
        {
            return Sprites.Where(sprite => sprite.Layer.Equals(layer, System.StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Gets all fade commands across all sprites
        /// </summary>
        /// <returns>All fade commands in the storyboard</returns>
        public IEnumerable<StoryboardCommand> GetAllFadeCommands()
        {
            return Sprites
                .SelectMany(sprite => sprite.Commands)
                .Where(cmd => cmd.CommandType == "F");
        }
        
        public override string ToString()
        {
            return $"Storyboard: {Sprites.Count} sprites, {TotalCommands} commands";
        }
    }
}