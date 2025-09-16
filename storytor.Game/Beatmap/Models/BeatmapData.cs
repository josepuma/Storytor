using System.Collections.Generic;

namespace storytor.Game.Beatmap.Models
{
    public class BeatmapData
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string BeatmapID { get; set; } = string.Empty;
        public string BeatmapSetID { get; set; } = string.Empty;

        public string AudioFilename { get; set; } = string.Empty;
        public int AudioLeadIn { get; set; }
        public int PreviewTime { get; set; }

        public string BackgroundImage { get; set; } = string.Empty;
        public string StoryboardFile { get; set; } = string.Empty;

        public float StackLeniency { get; set; }
        public int Mode { get; set; }
        public bool LetterboxInBreaks { get; set; }
        public bool UseSkinSprites { get; set; }
        public bool OverlayPosition { get; set; }
        public string SkinPreference { get; set; } = string.Empty;
        public bool EpilepsyWarning { get; set; }
        public int CountdownOffset { get; set; }
        public bool SpecialStyle { get; set; }
        public bool WidescreenStoryboard { get; set; }
        public bool SamplesMatchPlaybackRate { get; set; }

        public List<BeatmapEvent> Events { get; set; } = new List<BeatmapEvent>();
        public List<TimingPoint> TimingPoints { get; set; } = new List<TimingPoint>();

        public string FilePath { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;

        public string GetFullAudioPath()
        {
            if (string.IsNullOrEmpty(AudioFilename) || string.IsNullOrEmpty(FolderPath))
                return string.Empty;

            return System.IO.Path.Combine(FolderPath, AudioFilename);
        }

        public string GetFullBackgroundPath()
        {
            if (string.IsNullOrEmpty(BackgroundImage) || string.IsNullOrEmpty(FolderPath))
                return string.Empty;

            return System.IO.Path.Combine(FolderPath, BackgroundImage);
        }

        public string GetFullStoryboardPath()
        {
            if (string.IsNullOrEmpty(StoryboardFile) || string.IsNullOrEmpty(FolderPath))
                return string.Empty;

            return System.IO.Path.Combine(FolderPath, StoryboardFile);
        }

        public override string ToString()
        {
            return $"{Artist} - {Title} [{Version}] by {Creator}";
        }
    }

    public class BeatmapEvent
    {
        public string Type { get; set; } = string.Empty;
        public int StartTime { get; set; }
        public string[] Parameters { get; set; } = new string[0];

        public override string ToString()
        {
            return $"{Type} at {StartTime}ms: {string.Join(",", Parameters)}";
        }
    }

    public class TimingPoint
    {
        public int Time { get; set; }
        public double BeatLength { get; set; }
        public int Meter { get; set; }
        public int SampleSet { get; set; }
        public int SampleIndex { get; set; }
        public int Volume { get; set; }
        public bool Uninherited { get; set; }
        public int Effects { get; set; }

        public override string ToString()
        {
            return $"TimingPoint at {Time}ms: BeatLength={BeatLength}, Volume={Volume}";
        }
    }
}