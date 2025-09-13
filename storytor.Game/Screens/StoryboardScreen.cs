using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK.Input;
using storytor.Game.Storyboard;
using storytor.Game.Storyboard.Models;
using storytor.Game.Components;

namespace storytor.Game.Screens
{
    public partial class StoryboardScreen : Screen
    {
        [Resolved]
        private AudioManager audioManager { get; set; } = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        private Container storyboardContainer = null!;
        private SpriteText statusText = null!;
        private BasicButton loadButton = null!;
        private BasicButton playPauseButton = null!;

        private Track currentTrack;
        private StoryboardData currentStoryboard;
        private StoryboardRenderer storyboardRenderer;
        private TimelineComponent timeline;
        private GridOverlay gridOverlay;
        private ToggleSwitch gridToggle;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                // Background - dark fallback
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black
                },

                // Storyboard container - full screen background
                storyboardContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Direction = FillDirection.Vertical,
                            Spacing = new osuTK.Vector2(0, 10),
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Text = "Select a beatmap folder to load storyboard",
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Font = FontUsage.Default.With(size: 18),
                                    Colour = Colour4.White.Opacity(0.7f)
                                },
                                new SpriteText
                                {
                                    Text = "Controls: SPACE = Play/Pause | < > = Seek ±5s | ↑ ↓ = Seek ±10s",
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Font = FontUsage.Default.With(size: 14),
                                    Colour = Colour4.White.Opacity(0.5f)
                                }
                            }
                        }
                    }
                },

                // Grid overlay (on top of storyboard, behind UI)
                gridOverlay = new GridOverlay
                {
                    Alpha = 0
                },

                // Controls overlay - on top of storyboard
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(20),
                    Children = new Drawable[]
                    {
                        // Header controls
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Horizontal,
                            Spacing = new osuTK.Vector2(10, 0),
                            Children = new Drawable[]
                            {
                                loadButton = new BasicButton
                                {
                                    Text = "Select Beatmap Folder",
                                    Size = new osuTK.Vector2(200, 40),
                                    BackgroundColour = Colour4.DarkBlue,
                                    Action = selectFolder
                                },
                                playPauseButton = new BasicButton
                                {
                                    Text = "Play / Pause",
                                    Size = new osuTK.Vector2(120, 40),
                                    BackgroundColour = Colour4.DarkGreen,
                                    Action = togglePlayback,
                                    Alpha = 0.5f // Initially disabled
                                },
                                // Grid toggle
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Margin = new MarginPadding { Left = 20 },
                                    Children = new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Text = "Grid:",
                                            Font = FontUsage.Default.With(size: 14),
                                            Colour = Colour4.White,
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Margin = new MarginPadding { Right = 10 }
                                        },
                                        gridToggle = new ToggleSwitch
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            X = 35
                                        }
                                    }
                                }
                            }
                        },

                        // Status text at bottom (moved up for timeline)
                        statusText = new SpriteText
                        {
                            Text = "Ready to load a beatmap folder...",
                            Font = FontUsage.Default.With(size: 14),
                            Colour = Colour4.White,
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Margin = new MarginPadding { Bottom = 50 }
                        }
                    }
                },

                // Timeline component at bottom
                timeline = new TimelineComponent()
            };

            // Setup grid toggle functionality
            gridToggle.OnToggle += toggleGrid;
        }

        private void toggleGrid(bool enabled)
        {
            Console.WriteLine($"🔧 toggleGrid called with enabled: {enabled}");
            if (enabled)
                gridOverlay.Show();
            else
                gridOverlay.Hide();
        }

        protected override void Update()
        {
            base.Update();

            // Update time display and timeline
            if (currentTrack != null)
            {

                // Update timeline progress
                timeline.UpdateProgress(currentTrack.CurrentTime);

                // Update storyboard renderer with current time
                storyboardRenderer?.UpdateTime(currentTrack.CurrentTime);
            }
        }

        private async void selectFolder()
        {
            try
            {
                statusText.Text = "Opening folder picker...";
                loadButton.Enabled.Value = false;

                var folderPath = await showFolderPickerAsync();

                if (!string.IsNullOrEmpty(folderPath))
                {
                    // Clean up previous state
                    cleanupCurrentContent();

                    // Load new beatmap folder
                    await loadBeatmapFolderAsync(folderPath);
                }
                else
                {
                    statusText.Text = "No folder selected.";
                }
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error opening folder picker: {ex.Message}";
            }
            finally
            {
                loadButton.Enabled.Value = true;
            }
        }

        private void cleanupCurrentContent()
        {
            // Stop and dispose current track
            if (currentTrack != null)
            {
                currentTrack.Stop();
                currentTrack.Dispose();
                currentTrack = null;
            }

            // Clear current storyboard
            currentStoryboard = null;

            // Clear storyboard renderer
            if (storyboardRenderer != null)
            {
                storyboardContainer.Remove(storyboardRenderer, true);
                storyboardRenderer = null;
            }

            // Reset UI state
            playPauseButton.Alpha = 0.5f; // Disable play button

            // Reset storyboard container to initial state
            storyboardContainer.Clear();
            storyboardContainer.Add(new SpriteText
            {
                Text = "Loading new storyboard...",
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Font = FontUsage.Default.With(size: 18),
                Colour = Colour4.White.Opacity(0.7f)
            });
        }

        private async Task<string> showFolderPickerAsync()
        {
            try
            {
                // Create a task that will be completed when user selects a folder
                var taskCompletionSource = new TaskCompletionSource<string>();

                // Schedule the dialog on the main thread
                Schedule(() =>
                {
                    try
                    {
                        // Use platform-specific folder dialog
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // For Windows, we'll use a simpler approach since WinForms isn't available
                            var folderPath = showWindowsFolderPicker();
                            taskCompletionSource.SetResult(folderPath);
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            // For macOS, use osascript
                            var folderPath = showMacFolderPicker();
                            taskCompletionSource.SetResult(folderPath);
                        }
                        else
                        {
                            // For Linux/other, use zenity
                            var folderPath = showLinuxFolderPicker();
                            taskCompletionSource.SetResult(folderPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }
                });

                return await taskCompletionSource.Task;
            }
            catch (Exception)
            {
                // Fallback to simple input method
                statusText.Text = "Native dialog not available. Please enter path manually in console.";
                Console.WriteLine("Enter beatmap folder path:");
                return Console.ReadLine() ?? string.Empty;
            }
        }

        private static string showWindowsFolderPicker()
        {
            try
            {
                // Use PowerShell to show folder picker on Windows
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command \"Add-Type -AssemblyName System.Windows.Forms; $f = New-Object System.Windows.Forms.FolderBrowserDialog; $f.Description = 'Select beatmap folder'; $f.ShowDialog(); $f.SelectedPath\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                var result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                return process.ExitCode == 0 && !string.IsNullOrEmpty(result) ? result : string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing Windows folder picker: {ex.Message}");
                return string.Empty;
            }
        }

        private static string showMacFolderPicker()
        {
            try
            {
                // Use osascript to show macOS folder picker
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "osascript";
                process.StartInfo.Arguments = "-e \"POSIX path of (choose folder with prompt \\\"Select beatmap folder containing .osb and audio files\\\")\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                var result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                return process.ExitCode == 0 ? result : string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing macOS folder picker: {ex.Message}");
                return string.Empty;
            }
        }

        private static string showLinuxFolderPicker()
        {
            try
            {
                // Try to use zenity for Linux folder picker
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "zenity";
                process.StartInfo.Arguments = "--file-selection --directory --title=\"Select beatmap folder\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                var result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                return process.ExitCode == 0 ? result : string.Empty;
            }
            catch (Exception)
            {
                // Fallback: ask user to enter path manually
                Console.WriteLine("Enter beatmap folder path:");
                return Console.ReadLine() ?? string.Empty;
            }
        }

        private async Task loadBeatmapFolderAsync(string folderPath)
        {
            try
            {
                statusText.Text = "Loading beatmap folder...";
                loadButton.Enabled.Value = false;

                Console.WriteLine($"AudioManager available: {audioManager != null}");
                Console.WriteLine($"TrackStore available: {audioManager?.GetTrackStore() != null}");

                // Find audio files (try multiple formats)
                Console.WriteLine($"Scanning folder: {folderPath}");

                var mp3Files = Directory.GetFiles(folderPath, "*.mp3");
                var oggFiles = Directory.GetFiles(folderPath, "*.ogg");
                var wavFiles = Directory.GetFiles(folderPath, "*.wav");
                var m4aFiles = Directory.GetFiles(folderPath, "*.m4a");

                Console.WriteLine($"Found: {mp3Files.Length} mp3, {oggFiles.Length} ogg, {wavFiles.Length} wav, {m4aFiles.Length} m4a");

                var audioFiles = mp3Files
                    .Concat(oggFiles)
                    .Concat(wavFiles)
                    .Concat(m4aFiles)
                    .ToArray();

                if (audioFiles.Length == 0)
                {
                    statusText.Text = "No audio files found in folder!";
                    return;
                }

                // Find OSB file (optional for now)
                var osbFiles = Directory.GetFiles(folderPath, "*.osb");

                // Load audio
                var audioFile = audioFiles.First();
                statusText.Text = $"Loading audio: {Path.GetFileName(audioFile)}";

                try
                {
                    // Check if file exists and is readable
                    if (!File.Exists(audioFile))
                    {
                        statusText.Text = $"Audio file not found: {audioFile}";
                        return;
                    }

                    var fileInfo = new FileInfo(audioFile);
                    statusText.Text = $"Audio file size: {fileInfo.Length / 1024} KB";

                    // Create a custom track store for this specific folder
                    statusText.Text = "Creating custom track store...";

                    // Create a ResourceStore that can access files from the folder
                    var folderResourceStore = new StorageBackedResourceStore(host.GetStorage(folderPath));
                    var customTrackStore = audioManager.GetTrackStore(folderResourceStore);

                    // Get just the filename for the custom track store
                    var audioFileName = Path.GetFileName(audioFile);

                    Console.WriteLine($"Loading audio file: {audioFileName} from custom track store");
                    currentTrack = customTrackStore.Get(audioFileName);

                    // Setup timeline with track
                    if (currentTrack != null)
                    {
                        timeline.SetTrack(currentTrack);
                        timeline.OnSeek += seekToTime;
                    }

                    if (currentTrack == null)
                    {
                        statusText.Text = $"Failed to load: {audioFileName}. Trying fallback method...";

                        // Fallback: Try copying to Resources/Tracks/ temporarily
                        try
                        {
                            var resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tracks");
                            Directory.CreateDirectory(resourcesPath);

                            var tempFileName = $"temp_{Guid.NewGuid()}.mp3";
                            var tempPath = Path.Combine(resourcesPath, tempFileName);

                            File.Copy(audioFile, tempPath, true);

                            // Use global track store with the temp file
                            currentTrack = audioManager.GetTrackStore().Get(tempFileName);

                            if (currentTrack != null)
                            {
                                Console.WriteLine("Successfully loaded using Resources/Tracks/ fallback");

                                // Setup timeline with track
                                timeline.SetTrack(currentTrack);
                                timeline.OnSeek += seekToTime;

                                // Clean up temp file after track is loaded
                                // Note: We'll clean it up in Dispose()
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"Fallback method failed: {fallbackEx.Message}");
                        }
                    }

                    if (currentTrack == null)
                    {
                        // Get file details for debugging
                        Console.WriteLine($"File details:");
                        Console.WriteLine($"  Path: {audioFile}");
                        Console.WriteLine($"  Size: {fileInfo.Length} bytes");
                        Console.WriteLine($"  Extension: {fileInfo.Extension}");
                        Console.WriteLine($"  Exists: {fileInfo.Exists}");

                        statusText.Text = $"Cannot load audio: {Path.GetFileName(audioFile)}. Check console for details.";
                        return;
                    }

                    statusText.Text = $"Audio loaded: {currentTrack.Length}ms duration";
                }
                catch (Exception audioEx)
                {
                    statusText.Text = $"Audio loading error: {audioEx.Message}";
                    Console.WriteLine($"Audio error details: {audioEx}");
                    return;
                }

                // Load storyboard (if available)
                if (osbFiles.Length > 0)
                {
                    var osbFile = osbFiles.First();
                    statusText.Text = "Loading storyboard...";

                    currentStoryboard = await StoryboardLoader.LoadStoryboardAsync(osbFile);

                    if (currentStoryboard == null)
                    {
                        statusText.Text = "Failed to load storyboard, but audio is ready!";
                    }
                    else
                    {
                        // Create storyboard renderer
                        createStoryboardRenderer(folderPath);
                        statusText.Text = $"Loaded: {currentStoryboard.Sprites.Count} sprites, {currentStoryboard.TotalCommands} commands";
                    }
                }
                else
                {
                    statusText.Text = "Audio loaded! No storyboard found (.osb file).";
                }
                playPauseButton.Alpha = 1.0f; // Enable play button

            }
            catch (Exception ex)
            {
                statusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                loadButton.Enabled.Value = true;
            }
        }

        private void createStoryboardRenderer(string basePath)
        {
            if (currentStoryboard == null) return;

            // Clear existing content
            storyboardContainer.Clear();

            // Create new renderer
            storyboardRenderer = new StoryboardRenderer(currentStoryboard, basePath);
            storyboardContainer.Add(storyboardRenderer);
        }

        private void togglePlayback()
        {
            if (currentTrack == null) return;

            if (currentTrack.IsRunning)
            {
                currentTrack.Stop();
            }
            else
            {
                currentTrack.Start();
            }
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    if (currentTrack != null)
                    {
                        togglePlayback();
                        return true;
                    }
                    break;

                case Key.Left:
                    if (currentTrack != null)
                    {
                        seekTrack(-5000); // Retroceder 5 segundos
                        return true;
                    }
                    break;

                case Key.Right:
                    if (currentTrack != null)
                    {
                        seekTrack(5000); // Avanzar 5 segundos
                        return true;
                    }
                    break;

                case Key.Down:
                    if (currentTrack != null)
                    {
                        seekTrack(-10000); // Retroceder 10 segundos
                        return true;
                    }
                    break;

                case Key.Up:
                    if (currentTrack != null)
                    {
                        seekTrack(10000); // Avanzar 10 segundos
                        return true;
                    }
                    break;
            }

            return base.OnKeyDown(e);
        }

        private void seekTrack(double deltaMs)
        {
            if (currentTrack == null) return;

            var newTime = Math.Max(0, Math.Min(currentTrack.Length, currentTrack.CurrentTime + deltaMs));
            currentTrack.Seek(newTime);
        }

        private void seekToTime(double absoluteTimeMs)
        {
            if (currentTrack == null) return;

            var newTime = Math.Max(0, Math.Min(currentTrack.Length, absoluteTimeMs));
            currentTrack.Seek(newTime);
        }

        protected override void Dispose(bool isDisposing)
        {
            currentTrack?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
