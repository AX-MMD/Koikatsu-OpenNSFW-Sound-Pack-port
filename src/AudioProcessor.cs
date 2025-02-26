using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using NAudio.Wave;

namespace IllusionMods.Koikatsu3DSEModTools {

public static class AudioProcessor
{
    public static List<string> ValidFileExtensions = new List<string>(new string[] { ".wav", ".mp3" });

    public static void AdjustSilence(string filePath, int silenceDurationMs)
    {
        WaveFormat waveFormat;
        int silenceSamples;
        byte[] buffer;
        int bytesRead;

        if (Path.GetExtension(filePath).ToLower() == ".wav")
        {
            using (WaveFileReader reader = new WaveFileReader(filePath))
            {
                waveFormat = reader.WaveFormat;
                silenceSamples = (int)(silenceDurationMs * waveFormat.SampleRate / 1000);
                buffer = new byte[silenceSamples * waveFormat.BlockAlign];
                bytesRead = reader.Read(buffer, 0, buffer.Length);
            }

            if (bytesRead == 0)
            {
                Debug.LogWarning("No audio data found in " + filePath);
                return;
            }

            using (WaveFileWriter writer = new WaveFileWriter(filePath, waveFormat))
            {
                writer.Write(buffer, 0, bytesRead);
            }
        }
        else if (Path.GetExtension(filePath).ToLower() == ".mp3")
        {
            using (Mp3FileReader reader = new Mp3FileReader(filePath))
            {
                waveFormat = reader.WaveFormat;
                silenceSamples = (int)(silenceDurationMs * waveFormat.SampleRate / 1000);
                buffer = new byte[silenceSamples * waveFormat.BlockAlign];
                bytesRead = reader.Read(buffer, 0, buffer.Length);
            }

            if (bytesRead == 0)
            {
                Debug.LogWarning("No audio data found in " + filePath);
                return;
            }

            string tempWavPath = Path.ChangeExtension(filePath, ".temp.wav");
            using (WaveFileWriter writer = new WaveFileWriter(tempWavPath, waveFormat))
            {
                writer.Write(buffer, 0, bytesRead);
            }

            try
            {
                using (Mp3FileReader reader = new Mp3FileReader(tempWavPath))
                using (WaveFileWriter writer = new WaveFileWriter(filePath, waveFormat))
                {
                    buffer = new byte[reader.Length];
                    bytesRead = reader.Read(buffer, 0, buffer.Length);
                    writer.Write(buffer, 0, bytesRead);
                }
            }
            finally
            {
                File.Delete(tempWavPath);
            }
        }
    }

    public static bool IsSupportedAudioFile(string filePath)
    {
        return ValidFileExtensions.Contains(Path.GetExtension(filePath).ToLower());
    }
}