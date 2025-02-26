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
    private string silenceDurationStr = "0";

    public static void ShowWindow()
    {
        GetWindow<AdjustSilenceWindow>("Adjust Silence");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter silence duration in milliseconds (+/-):", EditorStyles.boldLabel);
        silenceDurationStr = EditorGUILayout.TextField("Duration (ms)", silenceDurationStr);

        if (GUILayout.Button("Adjust Silence"))
        {
            try
            {
                if (string.IsNullOrEmpty(silenceDurationStr))
                {
                    throw new Exception("Silence duration input was empty.");
                }

                int silenceDurationMs;
                if (!int.TryParse(silenceDurationStr, out silenceDurationMs))
                {
                    throw new Exception("Invalid silence duration input.");
                }

                string[] selectedPaths = GetSelectedPaths();
                if (selectedPaths.Length == 0)
                {
                    throw new Exception("No files or folders selected.");
                }

                foreach (string path in selectedPaths)
                {
                    if (Directory.Exists(path))
                    {
                        string[] allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (string file in allFiles)
                        {
                            if (AudioProcessor.IsValidAudioFile(file))
                            {
                                AudioProcessor.AdjustSilence(file, silenceDurationMs);
                            }
                        }
                    }
                    else if (File.Exists(path) && AudioProcessor.IsValidAudioFile(path))
                    {
                        AudioProcessor.AdjustSilence(path, silenceDurationMs);
                    }
                }
                EditorUtility.DisplayDialog("Success", "Silence adjustment completed.", "OK");
                this.Close();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
            finally
            {
                AssetDatabase.Refresh();
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