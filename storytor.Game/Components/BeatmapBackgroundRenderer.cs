using System;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osuTK;
using storytor.Game.Beatmap.Models;
using storytor.Game.Storyboard.Models;
using storytor.Game.Services;

namespace storytor.Game.Components
{
    public partial class BeatmapBackgroundRenderer : Container
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        private BeatmapData beatmap;
        private StoryboardData storyboard;
        private Sprite backgroundSprite;
        private bool isBackgroundDisabled;

        public BeatmapBackgroundRenderer(BeatmapData beatmap, StoryboardData storyboard = null)
        {
            this.beatmap = beatmap;
            this.storyboard = storyboard;

            RelativeSizeAxes = Axes.Both;
            Depth = float.MaxValue; // Ensure background is always at the back
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            checkIfBackgroundShouldBeDisabled();

            if (!isBackgroundDisabled && !string.IsNullOrEmpty(beatmap.BackgroundImage))
            {
                loadBackground();
            }
        }

        private void checkIfBackgroundShouldBeDisabled()
        {
            isBackgroundDisabled = false;

            if (storyboard == null || string.IsNullOrEmpty(beatmap.BackgroundImage))
                return;

            // Check if background image is used in storyboard
            var backgroundFilename = beatmap.BackgroundImage;

            foreach (var sprite in storyboard.Sprites)
            {
                if (sprite.ImagePath.Equals(backgroundFilename, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🚫 Background disabled: {backgroundFilename} is used in storyboard");
                    isBackgroundDisabled = true;
                    return;
                }
            }

            // Check for explicit background disable in storyboard
            // Look for: Sprite,Background,TopLeft,"background.jpg",0,0
            foreach (var sprite in storyboard.Sprites)
            {
                if (sprite.Layer == "Background" &&
                    sprite.ImagePath.Equals(backgroundFilename, StringComparison.OrdinalIgnoreCase) &&
                    sprite.Origin == Anchor.TopLeft &&
                    sprite.X == 0 &&
                    sprite.Y == 0 &&
                    sprite.Commands.Count == 0)
                {
                    Console.WriteLine($"🚫 Background disabled: Found explicit disable command for {backgroundFilename}");
                    isBackgroundDisabled = true;
                    return;
                }
            }

            Console.WriteLine($"✅ Background enabled: {backgroundFilename} will be rendered");
        }

        private void loadBackground()
        {
            try
            {
                var actualBackgroundPath = findBackgroundFile();

                if (string.IsNullOrEmpty(actualBackgroundPath))
                {
                    Console.WriteLine($"⚠️  Background file not found: {beatmap.BackgroundImage}");
                    Console.WriteLine($"   Searched in: {beatmap.FolderPath}");
                    listAvailableFiles();
                    return;
                }

                Console.WriteLine($"🔍 Found background file: {actualBackgroundPath}");

                // Load texture using shared cache service
                var texture = TextureCacheService.Instance.GetOrLoadTexture(actualBackgroundPath);

                if (texture == null)
                {
                    Console.WriteLine($"⚠️  Failed to load texture from cache service: {actualBackgroundPath}");
                    return;
                }

                // Calculate scale based on osu! widescreen setting
                // Widescreen: 854 / bg.width, Standard: 640 / bg.width
                float targetWidth = beatmap.WidescreenStoryboard ? 854f : 1024f;
                float scale = targetWidth / texture.Width;

                Console.WriteLine($"📐 Scale calculation:");
                Console.WriteLine($"   Widescreen: {beatmap.WidescreenStoryboard}");
                Console.WriteLine($"   Target width: {targetWidth}");
                Console.WriteLine($"   Background size: {texture.Width}x{texture.Height}");
                Console.WriteLine($"   Scale factor: {scale:F3}");

                backgroundSprite = new Sprite
                {
                    Texture = texture,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Scale = new osuTK.Vector2(scale),
                    RelativeSizeAxes = Axes.None,
                    FillMode = FillMode.Fill
                };

                Add(backgroundSprite);

                var actualFilename = Path.GetFileName(actualBackgroundPath);
                Console.WriteLine($"✅ Background loaded: {actualFilename}");
                Console.WriteLine($"   Final scaled size: {texture.Width * scale:F0}x{texture.Height * scale:F0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading background: {ex.Message}");
            }
        }

        private string findBackgroundFile()
        {
            if (string.IsNullOrEmpty(beatmap.BackgroundImage) || string.IsNullOrEmpty(beatmap.FolderPath))
                return string.Empty;

            // First try exact match
            var exactPath = beatmap.GetFullBackgroundPath();
            if (File.Exists(exactPath))
            {
                Console.WriteLine($"✅ Found exact match: {exactPath}");
                return exactPath;
            }

            // Try case-insensitive search
            var directory = beatmap.FolderPath;
            if (!Directory.Exists(directory))
                return string.Empty;

            var targetFilename = beatmap.BackgroundImage;
            var files = Directory.GetFiles(directory);

            // Try case-insensitive match
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (string.Equals(filename, targetFilename, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"✅ Found case-insensitive match: {file}");
                    Console.WriteLine($"   Looking for: {targetFilename}");
                    Console.WriteLine($"   Found: {filename}");
                    return file;
                }
            }

            // Try partial match (filename without extension)
            var targetNameOnly = Path.GetFileNameWithoutExtension(targetFilename);
            foreach (var file in files)
            {
                var fileNameOnly = Path.GetFileNameWithoutExtension(file);
                if (string.Equals(fileNameOnly, targetNameOnly, StringComparison.OrdinalIgnoreCase))
                {
                    var extension = Path.GetExtension(file).ToLower();
                    if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                    {
                        Console.WriteLine($"✅ Found partial match: {file}");
                        Console.WriteLine($"   Looking for: {targetFilename}");
                        Console.WriteLine($"   Found: {Path.GetFileName(file)}");
                        return file;
                    }
                }
            }

            return string.Empty;
        }

        private void listAvailableFiles()
        {
            try
            {
                if (!Directory.Exists(beatmap.FolderPath))
                    return;

                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                var imageFiles = Directory.GetFiles(beatmap.FolderPath)
                    .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .Select(Path.GetFileName)
                    .ToArray();

                Console.WriteLine($"📁 Available image files in folder:");
                foreach (var file in imageFiles)
                {
                    Console.WriteLine($"   - {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing files: {ex.Message}");
            }
        }

        public void UpdateBackgroundVisibility(bool visible)
        {
            if (backgroundSprite != null)
            {
                backgroundSprite.Alpha = visible ? 1.0f : 0.0f;
            }
        }

        public bool IsBackgroundActive => !isBackgroundDisabled && backgroundSprite != null;

        protected override void Dispose(bool isDisposing)
        {
            backgroundSprite?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
