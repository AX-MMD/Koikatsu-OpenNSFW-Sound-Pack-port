using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using IllusionMods.Koikatsu3DSEModTools;

public class AdjustAudio : MonoBehaviour
{
	[MenuItem("Assets/3DSE/Adjust Audio", true)]
	public static bool ValidateAdjust()
	{
		foreach (string guid in Selection.assetGUIDs)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!Directory.Exists(path) && !AudioProcessor.IsValidAudioFile(path))
			{
				return false;
			}
		}

		return Selection.assetGUIDs.Length > 0;
	}

	[MenuItem("Assets/3DSE/Adjust Audio")]
	public static void Adjust()
	{
		AdjustAudioWindow.ShowWindow();
	}
}

public class AdjustAudioWindow : EditorWindow
{
	private int silenceDurationMs;
	private int silenceMode;
	private int volumeMode;
	private int silenceThresholdDb;
	private int volumePercent;
	private int targetLoudnessDb;
	private string[] modes = new string[] { "Manual Duration", "Auto Normalize Duration", "Disabled" };
	private string[] volumeModes = new string[] { "Percent", "RMS Loudness Normalize", "Disabled" };
	private bool overwrite;
	private bool skipAssetReload;

	private const int MinVolumePercent = 0;
	private const int MaxVolumePercent = 300;

	private const string SelectedModeIndexKey = "AdjustAudio_SelectedModeIndex";
	private const string SoundThresholdKey = "AdjustAudio_SoundThreshold";
	private const string SilenceDurationKey = "AdjustAudio_SilenceDuration";

	private const string SelectedVolumeIndexKey = "AdjustAudio_SelectedVolumeIndex";
	private const string TargetLoudnessKey = "AdjustAudio_TargetLoudness";
	private const string VolumePercentKey = "AdjustAudio_VolumePercent";
	private const string OverwriteKey = "AdjustAudio_Overwrite";
	private const string SkipAssetReloadKey = "AdjustAudio_SkipAssetReload";

	public static void ShowWindow()
	{
		GetWindow<AdjustAudioWindow>("Adjust Audio");
	}

	private void OnEnable()
	{
		// Load saved values or set default values
		silenceMode = EditorPrefs.GetInt(SelectedModeIndexKey, 1);
		silenceDurationMs = EditorPrefs.GetInt(SilenceDurationKey, 60);
		silenceThresholdDb = EditorPrefs.GetInt(SoundThresholdKey, -50);
		volumeMode = EditorPrefs.GetInt(SelectedVolumeIndexKey, 1);
		volumePercent = EditorPrefs.GetInt(VolumePercentKey, 100);
		targetLoudnessDb = EditorPrefs.GetInt(TargetLoudnessKey, -32);
		overwrite = EditorPrefs.GetBool(OverwriteKey, true);
		skipAssetReload = EditorPrefs.GetBool(SkipAssetReloadKey, false);
	}

	private void OnDisable()
	{
		// Save values when the window is closed
		EditorPrefs.SetInt(SelectedModeIndexKey, silenceMode);
		EditorPrefs.SetInt(SilenceDurationKey, silenceDurationMs);
		EditorPrefs.SetInt(SoundThresholdKey, silenceThresholdDb);
		EditorPrefs.SetInt(SelectedVolumeIndexKey, volumeMode);
		EditorPrefs.SetInt(VolumePercentKey, volumePercent);
		EditorPrefs.SetInt(TargetLoudnessKey, targetLoudnessDb);
		EditorPrefs.SetBool(OverwriteKey, overwrite);
		EditorPrefs.SetBool(SkipAssetReloadKey, skipAssetReload);
	}

	private void OnGUI()
	{
		// UI
		GUILayout.Label("Silence Adjustement Method", EditorStyles.boldLabel);
		silenceMode = EditorGUILayout.Popup("Mode", silenceMode, modes);

		if (silenceMode != 2)
		{
			if (silenceMode == 1) // Auto Adjust
			{
				GUILayout.Label("Normalize initial silence duration (ms)", EditorStyles.boldLabel);
				silenceDurationMs = EditorGUILayout.IntField("Duration", silenceDurationMs);
			}
			else if (silenceMode == 0) // Manual
			{
				GUILayout.Label("Silence to add/remove at beginning (ms +/-)", EditorStyles.boldLabel);
				silenceDurationMs = EditorGUILayout.IntField("Duration (ms)", silenceDurationMs);
			}
			silenceThresholdDb = EditorGUILayout.IntSlider(
				string.Format("Silence Threshold (dB)", AudioProcessor.mindB, AudioProcessor.maxdB),
				silenceThresholdDb,
				AudioProcessor.mindB,
				AudioProcessor.maxdB
			);
		}

		GUILayout.Label("Volume Adjustment Method", EditorStyles.boldLabel);
		volumeMode = EditorGUILayout.Popup("Mode", volumeMode, volumeModes);

		if (volumeMode != 2)
		{
			if (volumeMode == 0) // Percent
			{
				GUILayout.Label(string.Format("Volume Adjustment ({0}%-{1}%)", MinVolumePercent, MaxVolumePercent), EditorStyles.boldLabel);
				volumePercent = EditorGUILayout.IntSlider("Volume (%)", volumePercent, MinVolumePercent, MaxVolumePercent);
			}
			else if (volumeMode == 1) // RMS Loudness Normalize
			{
				GUILayout.Label("RMS Loudness Normalize", EditorStyles.boldLabel);
				targetLoudnessDb = EditorGUILayout.IntSlider(
					string.Format("Loudness Target (dB)", AudioProcessor.mindB, AudioProcessor.maxdB),
					targetLoudnessDb,
					AudioProcessor.mindB,
					AudioProcessor.maxdB
				);
			}
		}

		GUILayout.Label("Options", EditorStyles.boldLabel);
		overwrite = EditorGUILayout.Toggle("Overwrite Files", overwrite);
		skipAssetReload = EditorGUILayout.Toggle("Skip Assets Refresh", skipAssetReload);

		if (this.IsValid() && GUILayout.Button("Adjust Audio"))
		{
			try
			{
				List<string> files = GetSelectedAudioFiles();
				if (files.Count == 0)
				{
					throw new Exception("No files or folders selected.");
				}

				float fileCount = 0.0f;
				foreach (string file in files)
				{
					fileCount++;
					EditorUtility.DisplayProgressBar("Adjusting files", string.Format("({0}/{1})", fileCount, files.Count), fileCount / files.Count);

					if (silenceMode != 2)
					{
						AudioProcessor.AdjustSilence(file, silenceDurationMs, silenceMode == 1, (sbyte)silenceThresholdDb, overwrite);
					}

					if (volumeMode == 0)
					{
						AudioProcessor.AdjustVolumePercent(file, (short)volumePercent, overwrite);
					}
					else if (volumeMode == 1)
					{
						AudioProcessor.NormalizeVolume(file, (sbyte)targetLoudnessDb, overwrite);
					}
				}

				if (!skipAssetReload && fileCount > 0)
				{
					AssetDatabase.Refresh();
				}
				EditorUtility.DisplayDialog("Success", string.Format("Adjustment completed for {0} files", fileCount), "OK");
				this.Close();
			}
			catch (Exception e)
			{
				Utils.LogErrorWithTrace(e);
				EditorUtility.DisplayDialog("Error", e.Message, "OK");
			}
			finally 
			{
				EditorUtility.ClearProgressBar();
			}
		}

		if (GUILayout.Button("Reset Defaults"))
		{
			ResetDefaults();
		}
	}

	private void ResetDefaults()
	{
		silenceMode = 1;
		silenceDurationMs = 60;
		silenceThresholdDb = -50;
		volumeMode = 1;
		volumePercent = 100;
		targetLoudnessDb = -32;
		overwrite = false;
		skipAssetReload = false;
	}

	private bool IsValid()
	{
		// Silence validation
		if (silenceMode != 2)
		{
			if (silenceMode == 1 && silenceDurationMs < 0)
			{
				EditorUtility.DisplayDialog("Validation Error", "When using auto adjust, Max silence duration must be positive.", "OK");
				return false;
			}
			// Sound threshold validation
			if (silenceThresholdDb > AudioProcessor.maxdB || silenceThresholdDb < AudioProcessor.mindB)
			{
				silenceThresholdDb = Mathf.Clamp(silenceThresholdDb, AudioProcessor.mindB, AudioProcessor.maxdB);
				EditorUtility.DisplayDialog("Validation Error", string.Format("Threshold must be between {0} and {1}.", AudioProcessor.mindB, AudioProcessor.maxdB), "OK");
				return false;
			}
		}

		// Volume percent validation
		if (volumeMode != 2)
		{
			if (volumeMode == 0 && (volumePercent < MinVolumePercent || volumePercent > MaxVolumePercent))
			{
				volumePercent = Mathf.Clamp(volumePercent, MinVolumePercent, MaxVolumePercent);
				EditorUtility.DisplayDialog("Validation Error", string.Format("Volume percent must be between {0} and {1}.", MinVolumePercent, MaxVolumePercent), "OK");
				return false;
			}
			else if (volumeMode == 1 && (targetLoudnessDb < AudioProcessor.mindB || targetLoudnessDb > AudioProcessor.maxdB))
			{
				targetLoudnessDb = Mathf.Clamp(targetLoudnessDb, AudioProcessor.mindB, AudioProcessor.maxdB);
				EditorUtility.DisplayDialog("Validation Error", string.Format("Target loudness must be between {0} and {1}.", AudioProcessor.mindB, AudioProcessor.maxdB), "OK");
				return false;
			}
		}

		return true;
	}

	private static List<string> GetSelectedAudioFiles()
	{
		List<string> paths = new List<string>();
		foreach (string path in Selection.assetGUIDs.Select(guid => AssetDatabase.GUIDToAssetPath(guid)))
		{
			string[] allFiles;
			if (Directory.Exists(path))
			{
				allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
			}
			else if (File.Exists(path))
			{
				allFiles = new string[] { path };
			}
			else
			{
				allFiles = new string[] { };
			}

			foreach (string file in allFiles)
			{
				if (AudioProcessor.IsValidAudioFile(file))
				{
					paths.Add(file);
				}
			}
		}

		return paths;
	}
}