using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Studio.Sound;

public class ExtractAudioFromBundles : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Extract Audio from Bundles in Folder")]
    public static void ExtractAudioInFolder()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("No folder selected.");
            return;
        }

        // Process all .unity3d files in the folder and subfolders
        string[] bundlePaths = Directory.GetFiles(folderPath, "*.unity3d", SearchOption.AllDirectories);
        foreach (string bundlePath in bundlePaths)
        {
            if (!bundlePath.Contains("unity3d"))
            {
                continue;
            }
            ExtractAudioFromBundle(bundlePath);
        }

        Debug.Log("All audio files extracted successfully.");
    }

    private static void ExtractAudioFromBundle(string bundlePath)
    {
        // Load the bundle
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            Debug.LogError("Failed to load AssetBundle: " + bundlePath);
            return;
        }

        // Extract all prefabs from the bundle
        string[] assetNames = bundle.GetAllAssetNames();
        List<GameObject> prefabs = new List<GameObject>();
        foreach (string assetName in assetNames)
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        // Create output folder
        string bundleName = Path.GetFileNameWithoutExtension(bundlePath);
        string outputFolder = Path.Combine(Path.GetDirectoryName(bundlePath), bundleName);
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Extract and save audio clips
        foreach (GameObject prefab in prefabs)
        {
            SEComponent seComponent = prefab.GetComponent<SEComponent>();
            if (seComponent != null && seComponent._clip != null)
            {
                string audioPath = AssetDatabase.GetAssetPath(seComponent._clip);
                string outputFilePath = Path.Combine(outputFolder, seComponent._clip.name + ".wav");
                File.Copy(audioPath, outputFilePath, true);
                Debug.Log("Extracted audio: " + outputFilePath);
            }
        }

        // Unload the bundle
        bundle.Unload(false);

        Debug.Log("Audio files extracted successfully: " + bundlePath);
    }
}