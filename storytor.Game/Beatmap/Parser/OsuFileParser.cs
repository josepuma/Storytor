using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using storytor.Game.Beatmap.Models;

namespace storytor.Game.Beatmap.Parser
{
    public static class OsuFileParser
    {
        public static async Task<BeatmapData> ParseAsync(string filePath)
        {
            return await Task.Run(() => ParseFile(filePath));
        }

        public static BeatmapData ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Beatmap file not found: {filePath}");
            }

            var beatmap = new BeatmapData
            {
                FilePath = filePath,
                FolderPath = Path.GetDirectoryName(filePath) ?? string.Empty
            };

            var lines = File.ReadAllLines(filePath);
            var currentSection = string.Empty;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    continue;
                }

                switch (currentSection.ToLower())
                {
                    case "general":
                        ParseGeneralSection(beatmap, trimmedLine);
                        break;
                    case "metadata":
                        ParseMetadataSection(beatmap, trimmedLine);
                        break;
                    case "events":
                        ParseEventsSection(beatmap, trimmedLine);
                        break;
                    case "timingpoints":
                        ParseTimingPointsSection(beatmap, trimmedLine);
                        break;
                }
            }

            return beatmap;
        }

        private static void ParseGeneralSection(BeatmapData beatmap, string line)
        {
            var parts = line.Split(new[] { ':' }, 2);
            if (parts.Length != 2) return;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case "AudioFilename":
                    beatmap.AudioFilename = value;
                    break;
                case "AudioLeadIn":
                    if (int.TryParse(value, out var leadIn))
                        beatmap.AudioLeadIn = leadIn;
                    break;
                case "PreviewTime":
                    if (int.TryParse(value, out var previewTime))
                        beatmap.PreviewTime = previewTime;
                    break;
                case "StackLeniency":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var stackLeniency))
                        beatmap.StackLeniency = stackLeniency;
                    break;
                case "Mode":
                    if (int.TryParse(value, out var mode))
                        beatmap.Mode = mode;
                    break;
                case "LetterboxInBreaks":
                    beatmap.LetterboxInBreaks = value == "1";
                    break;
                case "UseSkinSprites":
                    beatmap.UseSkinSprites = value == "1";
                    break;
                case "OverlayPosition":
                    beatmap.OverlayPosition = value == "NoChange";
                    break;
                case "SkinPreference":
                    beatmap.SkinPreference = value;
                    break;
                case "EpilepsyWarning":
                    beatmap.EpilepsyWarning = value == "1";
                    break;
                case "CountdownOffset":
                    if (int.TryParse(value, out var countdownOffset))
                        beatmap.CountdownOffset = countdownOffset;
                    break;
                case "SpecialStyle":
                    beatmap.SpecialStyle = value == "1";
                    break;
                case "WidescreenStoryboard":
                    beatmap.WidescreenStoryboard = value == "1";
                    break;
                case "SamplesMatchPlaybackRate":
                    beatmap.SamplesMatchPlaybackRate = value == "1";
                    break;
            }
        }

        private static void ParseMetadataSection(BeatmapData beatmap, string line)
        {
            var parts = line.Split(new[] { ':' }, 2);
            if (parts.Length != 2) return;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case "Title":
                    beatmap.Title = value;
                    break;
                case "Artist":
                    beatmap.Artist = value;
                    break;
                case "Creator":
                    beatmap.Creator = value;
                    break;
                case "Version":
                    beatmap.Version = value;
                    break;
                case "Source":
                    beatmap.Source = value;
                    break;
                case "Tags":
                    beatmap.Tags = value;
                    break;
                case "BeatmapID":
                    beatmap.BeatmapID = value;
                    break;
                case "BeatmapSetID":
                    beatmap.BeatmapSetID = value;
                    break;
            }
        }

        private static void ParseEventsSection(BeatmapData beatmap, string line)
        {
            var parts = line.Split(',');
            if (parts.Length == 0) return;

            var eventType = parts[0].Trim();

            switch (eventType)
            {
                case "0":
                    if (parts.Length >= 3)
                    {
                        var filename = parts[2].Trim('"');
                        if (!string.IsNullOrEmpty(filename))
                            beatmap.BackgroundImage = filename;
                    }
                    break;
                case "1":
                case "Video":
                    break;
                case "2":
                case "Break":
                    break;
                case "Storyboard Layer 0 (Background)":
                case "Storyboard Layer 1 (Fail)":
                case "Storyboard Layer 2 (Pass)":
                case "Storyboard Layer 3 (Foreground)":
                case "Storyboard Layer 4 (Overlay)":
                case "Storyboard Sound Samples":
                    break;
                default:
                    if (parts.Length >= 2)
                    {
                        var beatmapEvent = new BeatmapEvent
                        {
                            Type = eventType,
                            Parameters = parts.Skip(1).ToArray()
                        };

                        if (parts.Length >= 2 && int.TryParse(parts[1], out var startTime))
                            beatmapEvent.StartTime = startTime;

                        beatmap.Events.Add(beatmapEvent);
                    }
                    break;
            }
        }

        private static void ParseTimingPointsSection(BeatmapData beatmap, string line)
        {
            var parts = line.Split(',');
            if (parts.Length < 8) return;

            var timingPoint = new TimingPoint();

            if (int.TryParse(parts[0], out var time))
                timingPoint.Time = time;

            if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var beatLength))
                timingPoint.BeatLength = beatLength;

            if (parts.Length >= 3 && int.TryParse(parts[2], out var meter))
                timingPoint.Meter = meter;

            if (parts.Length >= 4 && int.TryParse(parts[3], out var sampleSet))
                timingPoint.SampleSet = sampleSet;

            if (parts.Length >= 5 && int.TryParse(parts[4], out var sampleIndex))
                timingPoint.SampleIndex = sampleIndex;

            if (parts.Length >= 6 && int.TryParse(parts[5], out var volume))
                timingPoint.Volume = volume;

            if (parts.Length >= 7)
                timingPoint.Uninherited = parts[6] == "1";

            if (parts.Length >= 8 && int.TryParse(parts[7], out var effects))
                timingPoint.Effects = effects;

            beatmap.TimingPoints.Add(timingPoint);
        }

        public static List<BeatmapData> FindAndParseOsuFiles(string folderPath)
        {
            var beatmaps = new List<BeatmapData>();

            if (!Directory.Exists(folderPath))
                return beatmaps;

            var osuFiles = Directory.GetFiles(folderPath, "*.osu");

            foreach (var osuFile in osuFiles)
            {
                try
                {
                    var beatmap = ParseFile(osuFile);
                    beatmaps.Add(beatmap);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse {osuFile}: {ex.Message}");
                }
            }

            return beatmaps;
        }

        public static BeatmapData FindBestBeatmap(string folderPath)
        {
            var beatmaps = FindAndParseOsuFiles(folderPath);

            if (!beatmaps.Any())
                return null;

            var beatmapWithStoryboard = beatmaps.FirstOrDefault(b => !string.IsNullOrEmpty(b.BackgroundImage) &&
                File.Exists(Path.Combine(folderPath, Path.ChangeExtension(Path.GetFileName(b.FilePath), ".osb"))));

            if (beatmapWithStoryboard != null)
            {
                var storyboardPath = Path.Combine(folderPath, Path.ChangeExtension(Path.GetFileName(beatmapWithStoryboard.FilePath), ".osb"));
                if (File.Exists(storyboardPath))
                    beatmapWithStoryboard.StoryboardFile = Path.GetFileName(storyboardPath);
                return beatmapWithStoryboard;
            }

            var beatmapWithBackground = beatmaps.FirstOrDefault(b => !string.IsNullOrEmpty(b.BackgroundImage));
            if (beatmapWithBackground != null)
                return beatmapWithBackground;

            return beatmaps.First();
        }
    }
}