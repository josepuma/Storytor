using System;
using System.IO;
using System.Threading.Tasks;

namespace storytor.Game.Storyboard.Reader
{
    /// <summary>
    /// Handles reading .osb files from disk with proper error handling
    /// </summary>
    public class OsbFileReader
    {
        /// <summary>
        /// Reads the content of an .osb file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the .osb file</param>
        /// <returns>The file content as a string array (one line per element)</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when the file is not a valid .osb file</exception>
        public static async Task<string[]> ReadOsbFileAsync(string filePath)
        {
            // Validate file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"OSB file not found: {filePath}");
            }
            
            // Validate file extension
            if (!Path.GetExtension(filePath).Equals(".osb", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"File is not an .osb file: {filePath}");
            }
            
            try
            {
                // Read all lines from the file
                string[] lines = await File.ReadAllLinesAsync(filePath);
                
                // Basic validation - check if it looks like a storyboard file
                if (lines.Length == 0)
                {
                    throw new InvalidOperationException("OSB file is empty");
                }
                
                return lines;
            }
            catch (Exception ex) when (!(ex is FileNotFoundException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Error reading OSB file: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Reads the content of an .osb file synchronously
        /// </summary>
        /// <param name="filePath">Path to the .osb file</param>
        /// <returns>The file content as a string array (one line per element)</returns>
        public static string[] ReadOsbFile(string filePath)
        {
            // Validate file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"OSB file not found: {filePath}");
            }
            
            // Validate file extension
            if (!Path.GetExtension(filePath).Equals(".osb", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"File is not an .osb file: {filePath}");
            }
            
            try
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);
                
                // Basic validation
                if (lines.Length == 0)
                {
                    throw new InvalidOperationException("OSB file is empty");
                }
                
                return lines;
            }
            catch (Exception ex) when (!(ex is FileNotFoundException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Error reading OSB file: {ex.Message}", ex);
            }
        }
    }
}