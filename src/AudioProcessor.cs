using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using NAudio.Wave;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IllusionMods.Koikatsu3DSEModTools
{
	public static class AudioProcessor
	{
		public static List<string> ValidFileExtensions = new List<string>(new string[] { ".wav", ".mp3" });
        public const float maxDb = 0.0f;
        public const float minDb = -60.0f;
		public static void AdjustSilence(string filePath, int silenceDurationMs, bool makeCopy = false)
		{
			WaveFormat waveFormat;
			int silenceSamples;
			int bytesPerSample;
			byte[] buffer;
			int bytesRead;

			if (Path.GetExtension(filePath).ToLower() == ".wav")
			{
				using (WaveFileReader reader = new WaveFileReader(filePath))
				{
					waveFormat = reader.WaveFormat;
					silenceSamples = (int)((waveFormat.SampleRate * Mathf.Abs(silenceDurationMs)) / 1000.0);
					bytesPerSample = waveFormat.BitsPerSample / 8;
					buffer = new byte[reader.Length];
					bytesRead = reader.Read(buffer, 0, buffer.Length);
				}

                string outputPath = makeCopy && !filePath.EndsWith(".tmp.wav") ? MakeOutputPath(filePath) : filePath;
				using (WaveFileWriter writer = new WaveFileWriter(outputPath, waveFormat))
				{
					if (silenceDurationMs > 0)
					{
						// Add silence
						byte[] silenceBuffer = new byte[silenceSamples * waveFormat.Channels * bytesPerSample];
						writer.Write(silenceBuffer, 0, silenceBuffer.Length);
						writer.Write(buffer, 0, bytesRead);
					}
					else if (silenceDurationMs < 0)
					{
						// Remove silence
						int bytesToRemove = silenceSamples * waveFormat.Channels * bytesPerSample;
						if (bytesToRemove < bytesRead)
						{
							writer.Write(buffer, bytesToRemove, bytesRead - bytesToRemove);
						}
						else
						{
							throw new Exception("Silence duration to remove is greater than the audio length.");
						}
					}
				}
			}
			else if (Path.GetExtension(filePath).ToLower() == ".mp3")
			{
				string tempWavPath = Path.ChangeExtension(filePath, ".temp.wav");

				// Convert MP3 to WAV
				using (Mp3FileReader reader = new Mp3FileReader(filePath))
				{
					waveFormat = reader.WaveFormat;
					using (WaveFileWriter writer = new WaveFileWriter(tempWavPath, waveFormat))
					{
						buffer = new byte[1024];
						while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
						{
							writer.Write(buffer, 0, bytesRead);
						}
					}
				}

				try 
				{
					// Adjust silence in WAV file
					AdjustSilence(tempWavPath, silenceDurationMs);

					// check if ffmpeg.exe is accessible in PATH
					if (System.Diagnostics.Process.Start("ffmpeg.exe", "-version") == null)
					{
						throw new Exception("ffmpeg.exe not found in PATH, it is required for MP3 manipulation.");
					}
                    string outputPath = makeCopy ? MakeOutputPath(filePath) : filePath;
                    string arguments = String.Format("-y -i \"{0}\" -codec:a libmp3lame -b:a 128k \"{1}\"", tempWavPath, outputPath);
					System.Diagnostics.Process.Start("ffmpeg.exe", arguments).WaitForExit();
				} 
				finally 
				{
					File.Delete(tempWavPath);
				}
			}
		}

		public static int AutoAdjustSilence(string filePath, int maxSilenceDurationMs, float thresholdDb = minDb, bool makeCopy = false)
		{
			int firstSoundTime = GetFirstSoundAboveThreshold(filePath, thresholdDb);
            int silenceDuration = maxSilenceDurationMs - firstSoundTime;
			if (firstSoundTime != -1 && Math.Abs(silenceDuration) > 1)
            {
                AdjustSilence(filePath, silenceDuration, makeCopy);
                return firstSoundTime;
            }
            else
            {
                return -1;
            }
		}

		public static string ConvertToWav(string filePath)
		{
			if (Path.GetExtension(filePath).ToLower() == ".wav")
			{
				return filePath;
			}

			string output = Path.ChangeExtension(filePath, ".wav");

			IWaveProvider waveProvider;
			using (var reader = GetReader(filePath, out waveProvider))
			{
				using (WaveFileWriter writer = new WaveFileWriter(output, waveProvider.WaveFormat))
				{
					byte[] buffer = new byte[1024];
					int bytesRead;
					while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
					{
						writer.Write(buffer, 0, bytesRead);
					}
				}
			}

			return output;
		}

		public static bool IsValidAudioFile(string filePath)
		{
			return ValidFileExtensions.Contains(Path.GetExtension(filePath).ToLower());
		}

		// Get the appropriate reader for the file type
		private static IDisposable GetReader(string filePath, out IWaveProvider waveProvider)
		{
			string extension = Path.GetExtension(filePath).ToLower();
			if (extension == ".wav")
			{
				var reader = new WaveFileReader(filePath);
				waveProvider = reader;
				return reader;
			}
			else if (extension == ".mp3")
			{
				var reader = new Mp3FileReader(filePath);
				waveProvider = reader;
				return reader;
			}
			else
			{
				throw new Exception(string.Format("Unsupported file type {0}, only MP3 and WAV are supported", extension));
			}
		}

		public static int GetFirstSoundAboveThreshold(string filePath, float thresholdDb = -60.0f)
        {
            IWaveProvider waveProvider;
            using (var reader = GetReader(filePath, out waveProvider))
            {
                var sampleProvider = waveProvider.ToSampleProvider();
                float[] buffer = new float[1024];
                int samplesRead;
                int totalSamples = 0;
                thresholdDb = Mathf.Clamp(thresholdDb, -60.0f, 0.0f);

                while ((samplesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < samplesRead; i++)
                    {
                        float sample = buffer[i];

                        // Calculate RMS value
                        float rms = sample * sample;

                        // Convert RMS to decibels
                        float decibel = 10 * (float)Math.Log10(rms);
                        if (decibel > thresholdDb)
                        {
                            double timeInSeconds = (double)totalSamples / sampleProvider.WaveFormat.SampleRate;
                            return (int)(timeInSeconds * 1000);
                        }

                        totalSamples++;
                    }
                }
            }

            return -1; // Return -1 if no sound above the threshold is found
        }

        private static string MakeOutputPath(string filePath)
        {
            string newFileName = Regex.Replace(
                Path.GetFileNameWithoutExtension(filePath), 
                @"%\d{14}%$", 
                "%" + DateTime.Now.ToString("yyyyMMddHHmmss") + "%"
            );
            return Path.Combine(Path.GetDirectoryName(filePath), newFileName + Path.GetExtension(filePath));
        }
    }
}