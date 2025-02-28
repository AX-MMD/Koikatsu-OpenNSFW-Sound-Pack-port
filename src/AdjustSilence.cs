using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using IllusionMods.Koikatsu3DSEModTools;

public class AdjustSilence : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Adjust Silence")]
    public static void Adjust()
    {
        AdjustSilenceWindow.ShowWindow();
    }
}

public class AdjustSilenceWindow : EditorWindow
{
    private string silenceDurationStr;
    private int selectedModeIndex;
    private string soundThresholdStr;
    private string[] modes = new string[] { "Manual", "Auto Adjust" };
    private bool overwrite;
    private bool skipAssetReload;

    private const string SilenceDurationKey = "AdjustSilence_SilenceDuration";
    private const string SelectedModeIndexKey = "AdjustSilence_SelectedModeIndex";
    private const string SoundThresholdKey = "AdjustSilence_SoundThreshold";
    private const string OverwriteKey = "AdjustSilence_Overwrite";
    private const string SkipAssetReloadKey = "AdjustSilence_SkipAssetReload";

    public static void ShowWindow()
    {
        GetWindow<AdjustSilenceWindow>("Adjust Silence");
    }

    private void OnEnable()
    {
        // Load saved values or set default values
        silenceDurationStr = EditorPrefs.GetString(SilenceDurationKey, "70");
        selectedModeIndex = EditorPrefs.GetInt(SelectedModeIndexKey, 1);
        soundThresholdStr = EditorPrefs.GetString(SoundThresholdKey, "-57.0");
        overwrite = EditorPrefs.GetBool(OverwriteKey, false);
        skipAssetReload = EditorPrefs.GetBool(SkipAssetReloadKey, false);
    }

    private void OnDisable()
    {
        // Save values when the window is closed
        EditorPrefs.SetString(SilenceDurationKey, silenceDurationStr);
        EditorPrefs.SetInt(SelectedModeIndexKey, selectedModeIndex);
        EditorPrefs.SetString(SoundThresholdKey, soundThresholdStr);
        EditorPrefs.SetBool(OverwriteKey, overwrite);
        EditorPrefs.SetBool(SkipAssetReloadKey, skipAssetReload);
    }

    private void OnGUI()
    {
        // UI
        GUILayout.Label("Method", EditorStyles.boldLabel);
        selectedModeIndex = EditorGUILayout.Popup("Mode", selectedModeIndex, modes);

        if (selectedModeIndex == 1) // Auto Adjust
        {
            GUILayout.Label("Max silence duration at start of clip (ms)", EditorStyles.boldLabel);
        }
        else // Manual
        {
            GUILayout.Label("Silence to add/remove at beginning (ms +/-)", EditorStyles.boldLabel);
        }

        silenceDurationStr = EditorGUILayout.TextField("Duration (ms)", silenceDurationStr);

        if (selectedModeIndex == 1)
        {
            GUILayout.Label(
                string.Format("Enter sound threshold (dB {0} to {1})", AudioProcessor.minDb, AudioProcessor.maxDb), 
                EditorStyles.boldLabel
            );
            soundThresholdStr = EditorGUILayout.TextField("Sound Threshold", soundThresholdStr);
        }

        GUILayout.Label("Options", EditorStyles.boldLabel);
        overwrite = EditorGUILayout.Toggle("Overwrite Files", overwrite);
        skipAssetReload = EditorGUILayout.Toggle("Skip Assets Refresh", skipAssetReload);

        if (GUILayout.Button("Adjust Silence"))
        {
            try
            {
                float thresholdDb;
                int silenceDurationMs;
                float fileCount = 0.0f;

                if (string.IsNullOrEmpty(silenceDurationStr))
                {
                    silenceDurationStr = EditorPrefs.GetString(SilenceDurationKey, "70");
                    throw new Exception("Silence duration input was empty.");
                }
                
                if (!int.TryParse(silenceDurationStr, out silenceDurationMs))
                {
                    silenceDurationStr = EditorPrefs.GetString(SilenceDurationKey, "70");
                    throw new Exception("Invalid silence duration input.");
                }
                else if (selectedModeIndex == 1 && silenceDurationMs < 0)
                {
                    throw new Exception("When using auto adjust, Max silence duration must be positive.");
                }

                if (!float.TryParse(soundThresholdStr, out thresholdDb))
                {
                    soundThresholdStr = EditorPrefs.GetString(SoundThresholdKey, "-57.0");
                    throw new Exception("Threshold was not a valid number.");
                }
                else if (thresholdDb > AudioProcessor.maxDb || thresholdDb < AudioProcessor.minDb)
                {
                    soundThresholdStr = thresholdDb > AudioProcessor.maxDb ? AudioProcessor.maxDb.ToString() : AudioProcessor.minDb.ToString();
                    throw new Exception(
                        string.Format("Threshold must be between {0} and {1}.", 
                        AudioProcessor.minDb, AudioProcessor.maxDb)
                    );
                }

                string[] selectedPaths = GetSelectedPaths();
                if (selectedPaths.Length == 0)
                {
                    throw new Exception("No files or folders selected.");
                }

                List<string> files = new List<string>();
                foreach (string path in selectedPaths)
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
                        throw new Exception("Invalid path selected: " + path);
                    }

                    foreach (string file in allFiles)
                    {
                        if (AudioProcessor.IsValidAudioFile(file))
                        {
                            files.Add(file);
                        }
                    }
                }

                foreach (string file in files)
                {
                    if (selectedModeIndex == 1)
                    {
                        if (-1 == AudioProcessor.AutoAdjustSilence(file, silenceDurationMs, thresholdDb, !overwrite))
                        {
                            Debug.Log("No adjustment needed: " + file);
                        }
                    }
                    else
                    {
                        AudioProcessor.AdjustSilence(file, silenceDurationMs, !overwrite);
                    }
                    
                    fileCount++;
                    EditorUtility.DisplayProgressBar("Adjusting files", fileCount + "/" + files.Count, fileCount / files.Count);
                }

                if (!skipAssetReload)
                {
                    AssetDatabase.Refresh();
                }
                EditorUtility.DisplayDialog("Success", string.Format("Adjustment completed for {0} files", fileCount), "OK");
                this.Close();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }
    }

    private static string[] GetSelectedPaths()
    {
        List<string> paths = new List<string>();
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                paths.Add(path);
            }
        }
        return paths.ToArray();
    }
}