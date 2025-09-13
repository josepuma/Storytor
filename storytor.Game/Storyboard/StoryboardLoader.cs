using System;
using System.Linq;
using System.Threading.Tasks;
using storytor.Game.Storyboard.Models;
using storytor.Game.Storyboard.Parser;

namespace storytor.Game.Storyboard
{
    /// <summary>
    /// High-level loader for storyboard files with logging and error handling
    /// </summary>
    public class StoryboardLoader
    {
        /// <summary>
        /// Loads a storyboard from an OSB file with comprehensive error handling
        /// </summary>
        /// <param name="filePath">Path to the .osb file</param>
        /// <returns>A loaded Storyboard object or null if loading fails</returns>
        public static async Task<StoryboardData> LoadStoryboardAsync(string filePath)
        {
            try
            {
                Console.WriteLine($"Loading storyboard from: {filePath}");
                
                var storyboard = await OsbParser.ParseAsync(filePath);
                
                Console.WriteLine($"✅ Successfully loaded storyboard:");
                Console.WriteLine($"   - {storyboard.Sprites.Count} sprites");
                Console.WriteLine($"   - {storyboard.TotalCommands} total commands");
                Console.WriteLine($"   - Layers used: {string.Join(", ", storyboard.UsedLayers)}");
                
                return storyboard;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load storyboard: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Loads a storyboard synchronously
        /// </summary>
        /// <param name="filePath">Path to the .osb file</param>
        /// <returns>A loaded Storyboard object or null if loading fails</returns>
        public static StoryboardData LoadStoryboard(string filePath)
        {
            try
            {
                Console.WriteLine($"Loading storyboard from: {filePath}");
                
                var storyboard = OsbParser.ParseFile(filePath);
                
                Console.WriteLine($"✅ Successfully loaded storyboard:");
                Console.WriteLine($"   - {storyboard.Sprites.Count} sprites");
                Console.WriteLine($"   - {storyboard.TotalCommands} total commands");
                Console.WriteLine($"   - Layers used: {string.Join(", ", storyboard.UsedLayers)}");
                
                return storyboard;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load storyboard: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Prints detailed information about a loaded storyboard
        /// </summary>
        /// <param name="storyboard">The storyboard to analyze</param>
        public static void PrintStoryboardDetails(StoryboardData storyboard)
        {
            Console.WriteLine("\n📊 Storyboard Analysis:");
            Console.WriteLine($"Source: {storyboard.FilePath}");
            Console.WriteLine($"Total Sprites: {storyboard.Sprites.Count}");
            Console.WriteLine($"Total Commands: {storyboard.TotalCommands}");
            // Count different command types
            var fadeCommands = storyboard.Sprites.SelectMany(s => s.Commands).Count(c => c.CommandType == "F");
            var moveCommands = storyboard.Sprites.SelectMany(s => s.Commands).Count(c => c.CommandType == "M");
            var scaleCommands = storyboard.Sprites.SelectMany(s => s.Commands).Count(c => c.CommandType == "S");
            var vectorScaleCommands = storyboard.Sprites.SelectMany(s => s.Commands).Count(c => c.CommandType == "V");
            var rotateCommands = storyboard.Sprites.SelectMany(s => s.Commands).Count(c => c.CommandType == "R");
            
            Console.WriteLine($"📊 Command Types:");
            Console.WriteLine($"   Fade: {fadeCommands}");
            Console.WriteLine($"   Move: {moveCommands}");
            Console.WriteLine($"   Scale: {scaleCommands}");
            Console.WriteLine($"   VectorScale: {vectorScaleCommands}");
            Console.WriteLine($"   Rotate: {rotateCommands}");
            
            // Show some sample commands for debugging
            if (rotateCommands > 0)
            {
                var firstRotate = storyboard.Sprites.SelectMany(s => s.Commands).First(c => c.CommandType == "R");
                Console.WriteLine($"   Sample Rotate: {firstRotate}");
            }
            
            if (vectorScaleCommands > 0)
            {
                var firstVectorScale = storyboard.Sprites.SelectMany(s => s.Commands).First(c => c.CommandType == "V");
                Console.WriteLine($"   Sample VectorScale: {firstVectorScale}");
            }
            
            Console.WriteLine("\n🖼️  Sprites by Layer:");
            foreach (var layer in storyboard.UsedLayers)
            {
                var spritesInLayer = storyboard.GetSpritesByLayer(layer);
                Console.WriteLine($"   {layer}: {spritesInLayer.Count()} sprites");
            }
            
            Console.WriteLine("\n📝 Sprite Details:");
            foreach (var sprite in storyboard.Sprites)
            {
                Console.WriteLine($"   • {sprite}");
                foreach (var command in sprite.Commands)
                {
                    Console.WriteLine($"     └─ {command}");
                }
            }
        }
    }
}