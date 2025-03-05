using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IllusionMods.Koikatsu3DSEModTools
{
	public static class AudioProcessor
	{
		public static List<string> ValidFileExtensions = new List<string>(new string[] { ".wav", ".mp3" });
		public const sbyte maxdB = 0;
		public const sbyte mindB = -60;
		public const sbyte baseVolume = -40;

		public static void AdjustSilence(string filePath, int silenceDurationMs, bool auto = true, sbyte silenceThreshold = mindB, bool overwrite = true)
		{
			if (Path.GetExtension(filePath).ToLower() == ".wav")
			{
				AdjustSilenceWav(filePath, silenceDurationMs, auto, silenceThreshold, overwrite);
			}
			else if (Path.GetExtension(filePath).ToLower() == ".mp3")
			{
				ProcessMp3(filePath, wavPath => AdjustSilenceWav(wavPath, silenceDurationMs, auto, silenceThreshold, true), overwrite);
			}
			else
			{
				throw new Exception("Unsupported file type, only MP3 and WAV are supported");
			}
		}

		public static void AdjustSilenceWav(string filePath, int silenceDurationMs, bool auto = true, sbyte silenceThreshold = mindB, bool overwrite = true)
		{
			string outputFilePath = GetFileCopyPath(filePath);
			int silenceAdjustement = 0;

			using (AudioFileReader reader = new AudioFileReader(filePath))
			{
				TimeSpan duration = reader.GetSilenceDuration(NAudioFileReaderExt.SilenceLocation.Start, silenceThreshold);

				if (auto)
				{
					silenceAdjustement = silenceDurationMs - (int)duration.TotalMilliseconds;
				}
				else if (silenceDurationMs < 0 && (int)duration.TotalMilliseconds < Mathf.Abs(silenceDurationMs))
				{
					silenceAdjustement = -(int)duration.TotalMilliseconds;
				}
				else
				{
					silenceAdjustement = silenceDurationMs;
				}

				// Skip adjustment if it's less then 1ms to prevent file corruption
				if (silenceAdjustement > 1)
				{
					WaveFormat waveFormat = reader.WaveFormat;
					SilenceProvider silenceProvider = new SilenceProvider(waveFormat);
					OffsetSampleProvider offsetProvider = new OffsetSampleProvider(silenceProvider.ToSampleProvider())
					{
						TakeSamples = waveFormat.SampleRate * silenceAdjustement / 1000 * waveFormat.Channels
					};

					ConcatenatingSampleProvider concatenated = new ConcatenatingSampleProvider(new ISampleProvider[] { offsetProvider, reader });
					try
					{
						WaveFileWriter.CreateWaveFile(outputFilePath, new SampleToWaveProvider(concatenated));
					}
					catch (DirectoryNotFoundException) 
					{
						outputFilePath = filePath + "(1)";
						WaveFileWriter.CreateWaveFile (outputFilePath, new SampleToWaveProvider (concatenated));
					}
				}
				else if (silenceAdjustement < -1)
				{
					WaveFormat waveFormat = reader.WaveFormat;
					int bytesPerMillisecond = waveFormat.AverageBytesPerSecond / 1000;
					int bytesToTrim = Math.Abs(silenceAdjustement) * bytesPerMillisecond;

					reader.Position = bytesToTrim;
					try
					{
						WaveFileWriter.CreateWaveFile(outputFilePath, reader);
					}
					catch (DirectoryNotFoundException)
					{
						outputFilePath = filePath+"(1)";
						WaveFileWriter.CreateWaveFile(outputFilePath, reader);
					}
				}
				else
				{
					outputFilePath = "";
					Debug.LogWarning("Silence is already adjusted: " + filePath);
				}
			}

			if (overwrite && File.Exists(outputFilePath))
			{
				File.Delete(filePath);
				File.Move(outputFilePath, filePath);
				if (File.Exists(outputFilePath + ".meta"))
				{
					File.Move(outputFilePath + ".meta", filePath + ".meta");
				}
			}
		}

		public static void AdjustVolumePercent(string filePath, short percent, bool overwrite = true)
		{
			if (percent == 100)
			{
				return;
			}
			else if (Path.GetExtension(filePath).ToLower() == ".wav")
			{
				AdjustVolumePercentWav(filePath, percent, overwrite);
			}
			else if (Path.GetExtension(filePath).ToLower() == ".mp3")
			{
				ProcessMp3(filePath, wavPath => AdjustVolumePercentWav(wavPath, percent, true), overwrite);
			}
		}

		public static void AdjustVolumePercentWav(string filePath, short percent, bool overwrite = true)
		{
			string outputPath = GetFileCopyPath(filePath);
			using (var reader = new AudioFileReader(filePath))
			{
				reader.Volume = percent / 100.0f;
				try
				{
					WaveFileWriter.CreateWaveFile(outputPath, reader);
				}
				catch (DirectoryNotFoundException) 
				{
					outputPath = filePath + "(1)";
					WaveFileWriter.CreateWaveFile (outputPath, reader);
				}
			}

			if (overwrite && File.Exists(outputPath))
			{
				File.Delete(filePath);
				File.Move(outputPath, filePath);
				if (File.Exists(outputPath + ".meta"))
				{
					File.Move(outputPath + ".meta", filePath + ".meta");
				}
			}
		}

		public static void NormalizeVolume(string inputFilePath, sbyte targetRmsDb, bool overwrite = true)
		{
			if (Path.GetExtension(inputFilePath).ToLower() == ".wav")
			{
				NormalizeVolumeWav(inputFilePath, targetRmsDb, overwrite);
			}
			else if (Path.GetExtension(inputFilePath).ToLower() == ".mp3")
			{
				ProcessMp3(inputFilePath, wavPath => NormalizeVolumeWav(wavPath, targetRmsDb, true), overwrite);
			}
		}

		public static void NormalizeVolumeWav(string inputFilePath, sbyte targetRmsDb = baseVolume, bool overwrite = true)
		{
			string outputFilePath = GetFileCopyPath(inputFilePath);
			using (AudioFileReader reader = new AudioFileReader(inputFilePath))
			{
				var sampleProvider = reader.ToSampleProvider();
				float sumOfSquares = 0;
				long totalSamples = 0;

				// Calculate the RMS value of the audio
				float[] buffer = new float[1024];
				int samplesRead;
				while ((samplesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
				{
					for (int i = 0; i < samplesRead; i++)
					{
						sumOfSquares += buffer[i] * buffer[i];
						totalSamples++;
					}
				}

				float rms = (float)Math.Sqrt(sumOfSquares / totalSamples);
				float rmsDb = 20 * (float)Math.Log10(rms);

				if (Math.Abs(rmsDb - targetRmsDb) < 0.1)
				{
					outputFilePath = "";
					Debug.LogWarning("Audio is already normalized: " + inputFilePath);
				}
				else
				{
					// Calculate the gain factor
					float gainFactor = (float)Math.Pow(10, (targetRmsDb - rmsDb) / 20);

					// Apply the gain factor
					reader.Position = 0;
					var gainProvider = new VolumeSampleProvider(sampleProvider) { Volume = gainFactor };
					try
					{
						WaveFileWriter.CreateWaveFile(outputFilePath, new SampleToWaveProvider(gainProvider));
					}
					catch (DirectoryNotFoundException) 
					{
						outputFilePath = inputFilePath + "(1)";
						WaveFileWriter.CreateWaveFile (outputFilePath, new SampleToWaveProvider (gainProvider));
					}
				}
			}

			if (overwrite && File.Exists(outputFilePath))
			{
				File.Delete(inputFilePath);
				File.Move(outputFilePath, inputFilePath);
				if (File.Exists(outputFilePath + ".meta"))
				{
					File.Move(outputFilePath + ".meta", inputFilePath + ".meta");
				}
			}
		}

		public static void ProcessMp3(string filePath, Action<string> wavFunction, bool overwrite = true)
		{
			string tempWavPath = Path.ChangeExtension(filePath, ".temp.wav");
			try
			{
				// Convert MP3 to WAV
				using (Mp3FileReader reader = new Mp3FileReader(filePath))
				{
					WaveFileWriter.CreateWaveFile(tempWavPath, reader);
				}

				// Execute the provided WAV function
				wavFunction(tempWavPath);

				// Convert WAV back to MP3
				if (System.Diagnostics.Process.Start("ffmpeg.exe", "-version") == null)
				{
					throw new Exception("ffmpeg.exe not found in PATH, it is required for MP3 manipulation.");
				}
				string outputPath = overwrite ? filePath : GetFileCopyPath(filePath);
				string arguments = String.Format("-y -i \"{0}\" -codec:a libmp3lame -b:a 128k \"{1}\"", tempWavPath, outputPath);
				System.Diagnostics.Process.Start("ffmpeg.exe", arguments).WaitForExit();
			}
			finally
			{
				File.Delete(tempWavPath);
				File.Delete(tempWavPath + ".meta");
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
			return File.Exists(filePath) && ValidFileExtensions.Contains(Path.GetExtension(filePath).ToLower());
		}

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

		// Deprecated for NAudioFileReaderExt
		public static int GetFirstSoundAboveThreshold(string filePath, int thresholdDb = -60)
		{
			IWaveProvider waveProvider;
			using (var reader = GetReader(filePath, out waveProvider))
			{
				var sampleProvider = waveProvider.ToSampleProvider();
				float[] buffer = new float[1024];
				int samplesRead;
				int totalSamples = 0;

				if (thresholdDb > maxdB || thresholdDb < mindB)
				{
					thresholdDb = Mathf.Clamp(thresholdDb, mindB, maxdB);
					Debug.LogWarning(string.Format("Threshold out off range ({0}dB, {1}dB) was corrected to closest value.", mindB, maxdB));
				}

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

		private static string GetFileCopyPath(string filePath)
		{
			string name = Path.GetFileNameWithoutExtension(filePath);
			Match match = Regex.Match(name, @"_\d{4}\d{4}\d{2}\d{4}$");
			if (match.Success)
			{
				Regex.Replace(name, @"\d{4}\d{4}\d{2}\d{4}$", DateTime.Now.ToString("yyyyMMddHHmmss"));
			}
			else
			{
				name += "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
			}

			return Path.Combine(Path.GetDirectoryName(filePath), name + Path.GetExtension(filePath));
		}
	}

	// From https://stackoverflow.com/a/46024371
	public static class NAudioFileReaderExt
	{
		public enum SilenceLocation { Start, End }

		private static bool IsSilence(float amplitude, sbyte threshold)
		{
			double dB = 20 * Math.Log10(Math.Abs(amplitude));
			return dB < threshold;
		}

		public static TimeSpan GetSilenceDuration(
			this AudioFileReader reader, 
			SilenceLocation location, 
			sbyte silenceThreshold = -40
		) {
			int counter = 0;
			bool volumeFound = false;
			bool eof = false;
			long oldPosition = reader.Position;

			var buffer = new float[reader.WaveFormat.SampleRate * 4];
			while (!volumeFound && !eof)
			{
				int samplesRead = reader.Read(buffer, 0, buffer.Length);
				if (samplesRead == 0)
					eof = true;

				for (int n = 0; n < samplesRead; n++)
				{
					if (IsSilence(buffer[n], silenceThreshold))
					{
						counter++;
					}
					else
					{
						if (location == SilenceLocation.Start)
						{
							volumeFound = true;
							break;
						}
						else if (location == SilenceLocation.End)
						{
							counter = 0;
						}
					}
				}
			}

			// reset position
			reader.Position = oldPosition;

			double silenceSamples = (double)counter / reader.WaveFormat.Channels;
			double silenceDuration = (silenceSamples / reader.WaveFormat.SampleRate) * 1000;
			return TimeSpan.FromMilliseconds(silenceDuration);
		}
	}
}