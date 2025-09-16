using System.Collections.Generic;
using osu.Framework.Graphics;

namespace storytor.Game.Storyboard.Models
{
    /// <summary>
    /// Represents a sprite in the storyboard with its properties and commands
    /// </summary>
    public class StoryboardSprite
    {
        /// <summary>
        /// Unique identifier for this sprite instance
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The layer this sprite belongs to (Background, Fail, Pass, Foreground, Overlay)
        /// </summary>
        public string Layer { get; set; } = string.Empty;
        
        /// <summary>
        /// The origin point of the sprite (TopLeft, Centre, etc.)
        /// </summary>
        public Anchor Origin { get; set; }
        
        /// <summary>
        /// Path to the sprite image file
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Initial X position of the sprite
        /// </summary>
        public float X { get; set; }
        
        /// <summary>
        /// Initial Y position of the sprite
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Indicates if this is an animation sprite
        /// </summary>
        public bool IsAnimation { get; set; }

        /// <summary>
        /// Number of frames in the animation (animations only)
        /// </summary>
        public int FrameCount { get; set; }

        /// <summary>
        /// Delay in milliseconds between each frame (animations only)
        /// </summary>
        public double FrameDelay { get; set; }

        /// <summary>
        /// Loop type for the animation (animations only)
        /// LoopForever, LoopOnce
        /// </summary>
        public string LoopType { get; set; } = "LoopForever";

        /// <summary>
        /// List of commands applied to this sprite
        /// </summary>
        public List<StoryboardCommand> Commands { get; set; } = new List<StoryboardCommand>();
        
        /// <summary>
        /// Gets the display name of the sprite (filename without path)
        /// </summary>
        public string DisplayName => System.IO.Path.GetFileName(ImagePath);
        
        public override string ToString()
        {
            return $"Sprite: {DisplayName} at ({X}, {Y}) - {Commands.Count} commands";
        }
    }
}