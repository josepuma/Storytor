using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;

namespace storytor.Game.Services
{
    /// <summary>
    /// Global texture cache service to optimize texture loading across the application
    /// </summary>
    public class TextureCacheService
    {
        private static TextureCacheService instance;
        private readonly Dictionary<string, Texture> textureCache = new();
        private readonly GameHost host;

        private TextureCacheService(GameHost host)
        {
            this.host = host;
        }

        public static void Initialize(GameHost host)
        {
            instance = new TextureCacheService(host);
        }

        public static TextureCacheService Instance => instance ?? throw new InvalidOperationException("TextureCacheService not initialized");

        /// <summary>
        /// Gets or loads a texture from the cache
        /// </summary>
        /// <param name="filePath">Full path to the image file</param>
        /// <returns>Cached or newly loaded texture, null if failed</returns>
        public Texture GetOrLoadTexture(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            // Normalize path for consistent caching
            var normalizedPath = Path.GetFullPath(filePath);

            // Check cache first
            if (textureCache.TryGetValue(normalizedPath, out var cachedTexture))
            {
                //Console.WriteLine($"📦 Texture cache HIT: {Path.GetFileName(normalizedPath)}");
                return cachedTexture;
            }

            // Load new texture
            try
            {
                using var stream = File.OpenRead(normalizedPath);
                var texture = Texture.FromStream(host.Renderer, stream);

                if (texture != null)
                {
                    textureCache[normalizedPath] = texture;
                    //Console.WriteLine($"📥 Texture cache MISS, loaded: {Path.GetFileName(normalizedPath)} ({texture.Width}x{texture.Height})");
                    //Console.WriteLine($"📊 Cache size: {textureCache.Count} textures");
                }

                return texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load texture {Path.GetFileName(normalizedPath)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Preloads a texture into the cache without returning it
        /// </summary>
        /// <param name="filePath">Full path to the image file</param>
        /// <returns>True if successfully loaded and cached</returns>
        public bool PreloadTexture(string filePath)
        {
            var texture = GetOrLoadTexture(filePath);
            return texture != null;
        }

        /// <summary>
        /// Removes a texture from the cache and disposes it
        /// </summary>
        /// <param name="filePath">Full path to the image file</param>
        public void RemoveTexture(string filePath)
        {
            var normalizedPath = Path.GetFullPath(filePath);

            if (textureCache.TryGetValue(normalizedPath, out var texture))
            {
                texture.Dispose();
                textureCache.Remove(normalizedPath);
                Console.WriteLine($"🗑️  Removed texture from cache: {Path.GetFileName(normalizedPath)}");
            }
        }

        /// <summary>
        /// Clears all cached textures and disposes them
        /// </summary>
        public void ClearCache()
        {
            Console.WriteLine($"🧹 Clearing texture cache ({textureCache.Count} textures)");

            foreach (var texture in textureCache.Values)
            {
                texture.Dispose();
            }

            textureCache.Clear();
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        /// <returns>Dictionary with cache stats</returns>
        public Dictionary<string, object> GetCacheStats()
        {
            var totalMemory = 0L;
            foreach (var texture in textureCache.Values)
            {
                // Approximate memory usage: width * height * 4 bytes (RGBA)
                totalMemory += texture.Width * texture.Height * 4;
            }

            return new Dictionary<string, object>
            {
                ["TextureCount"] = textureCache.Count,
                ["EstimatedMemoryMB"] = totalMemory / (1024.0 * 1024.0),
                ["CachedPaths"] = string.Join(", ", textureCache.Keys.Select(Path.GetFileName))
            };
        }

        /// <summary>
        /// Checks if a texture is cached
        /// </summary>
        /// <param name="filePath">Full path to the image file</param>
        /// <returns>True if texture is in cache</returns>
        public bool IsTextureCached(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var normalizedPath = Path.GetFullPath(filePath);
            return textureCache.ContainsKey(normalizedPath);
        }

        /// <summary>
        /// Disposes the service and clears all cached textures
        /// </summary>
        public void Dispose()
        {
            ClearCache();
        }
    }
}
