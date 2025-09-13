using System;
using System.IO;

namespace storytor.Game.Utils
{
    /// <summary>
    /// Utility to generate simple test audio files for debugging
    /// </summary>
    public static class AudioTestGenerator
    {
        /// <summary>
        /// Creates a simple WAV file with a sine wave tone for testing
        /// </summary>
        /// <param name="filePath">Where to save the test audio file</param>
        /// <param name="durationSeconds">Duration in seconds</param>
        /// <param name="frequency">Frequency of the tone in Hz</param>
        public static void CreateTestWavFile(string filePath, double durationSeconds = 5.0, double frequency = 440.0)
        {
            var sampleRate = 44100;
            var samples = (int)(sampleRate * durationSeconds);
            var amplitude = 16383; // Half of 16-bit range
            
            using var fileStream = new FileStream(filePath, FileMode.Create);
            using var writer = new BinaryWriter(fileStream);
            
            // WAV header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + samples * 2); // File size - 8
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16); // Subchunk size
            writer.Write((short)1); // PCM format
            writer.Write((short)1); // Mono
            writer.Write(sampleRate); // Sample rate
            writer.Write(sampleRate * 2); // Byte rate
            writer.Write((short)2); // Block align
            writer.Write((short)16); // Bits per sample
            writer.Write("data".ToCharArray());
            writer.Write(samples * 2); // Data size
            
            // Generate sine wave data
            for (int i = 0; i < samples; i++)
            {
                var time = (double)i / sampleRate;
                var sample = Math.Sin(2 * Math.PI * frequency * time) * amplitude;
                writer.Write((short)sample);
            }
            
            Console.WriteLine($"Created test WAV file: {filePath} ({durationSeconds}s, {frequency}Hz)");
        }
        
        /// <summary>
        /// Creates a test audio file in the specified folder
        /// </summary>
        /// <param name="folderPath">Target folder</param>
        /// <returns>Path to the created test file</returns>
        public static string CreateTestAudioInFolder(string folderPath)
        {
            var testFilePath = Path.Combine(folderPath, "test_audio.wav");
            CreateTestWavFile(testFilePath, 10.0, 440.0); // 10 second A4 note
            return testFilePath;
        }
    }
}