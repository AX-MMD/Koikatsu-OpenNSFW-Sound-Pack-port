using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Studio.Sound;


// This script generates prefabs from .wav files in a specified folder.

public class GeneratePrefabsFromWav : MonoBehaviour
{
    [MenuItem("Assets/Generate Prefabs From WAV")]
    public static void GeneratePrefabs()
    {
		string folderPath = GetSelectedFolderPath();
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Please select a folder in the Assets directory.");
            return;
        }

        string outputPath = "Assets/Mods/Prefab";
        string basePrefabPath = "Assets/3DSE objects/base_3dse.prefab";

        Debug.Log("Starting prefab generation...");
        Debug.Log("Folder path: " + folderPath);
        Debug.Log("Output path: " + outputPath);
        Debug.Log("Base prefab path: " + basePrefabPath);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Folder path does not exist: " + folderPath);
            return;
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            Debug.Log("Created output directory: " + outputPath);
        }
		else
        {
            // Clear the output directory
            DirectoryInfo directoryInfo = new DirectoryInfo(outputPath);
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }
            Debug.Log("Cleared output directory: " + outputPath);
        }

        UnityEngine.Object basePrefab = AssetDatabase.LoadAssetAtPath(basePrefabPath, typeof(GameObject));
        if (basePrefab == null)
        {
            Debug.LogError("Base prefab not found at path: " + basePrefabPath);
            return;
        }

        // string[] wavFiles = GetAllWavFiles(folderPath);
        string[] wavFiles = Directory.GetFiles(folderPath, "*.wav");
        string folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        int i = 1;

        foreach (string wavFile in wavFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(wavFile);
            string itemName = GetItemName(fileName, folderName);
            string newPrefabName = string.Format("{0}.prefab", itemName);
            string newPrefabPath = Path.Combine(outputPath, newPrefabName).Replace("\\", "/");

            Debug.Log("Creating prefab: " + newPrefabPath);

            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);

            // Load the AudioClip from the .wav file
            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavFile);
            if (audioClip == null)
            {
                Debug.LogError("AudioClip not found at path: " + wavFile);
                continue;
            }

            // Assign the AudioClip to the SEComponent
            SEComponent seComponent = newObject.GetComponent<SEComponent>();
            if (seComponent != null)
            {
                seComponent._clip = audioClip;
            }
            else
            {
                Debug.LogError("SEComponent not found on the instantiated prefab.");
            }

            PrefabUtility.CreatePrefab(newPrefabPath, newObject);
            DestroyImmediate(newObject);

            Debug.Log("Prefab created: " + newPrefabPath);

            i++;
        }

        AssetDatabase.Refresh();
        Debug.Log(string.Format("{0} Prefabs generated successfully.", i - 1));
    }

    private static string GetSelectedFolderPath()
    {
        if (Selection.activeObject == null)
        {
            Debug.LogError("Selection is empty");
            return null;
        }

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path)) return null;

        if (Directory.Exists(path))
        {
            return path;
        }
        else
        {
            return Path.GetDirectoryName(path);
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string snakeCase = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower();
        snakeCase = Regex.Replace(snakeCase, @"\s+", "_");
        return snakeCase;
    }

    private static string GetItemName(string fileName, string folderName)
    {
        /*
        *  Adjust this function according to your needs
        */

		// Match match = Regex.Match(fileName, @"^([A-z\s]*-0*)(\d+)$");
		// Match match = Regex.Match(fileName, @"^(.*)\s*-\s*(\d+)$");
		// Match match = Regex.Match(fileName, @"^(.*)\s*\((\d+)\)$");
		// Match match = Regex.Match(fileName, @"^(.*)\s*-\s*\((\d+)\)$");
		// Match match = Regex.Match(fileName, @"^(.*)_(\d+)$");
        // string itemNamePrefix = ToSnakeCase(folderName);
        // if (match.Success)
        // {
        //     return string.Format("{0}{1}", itemNamePrefix, match.Groups[2].Value)
        // }
        return fileName;
    }

    static string[] GetAllWavFiles(string folderPath)
    {
        List<string> wavFiles = new List<string>();
        GetWavFilesRecursive(folderPath, wavFiles);
        return wavFiles.ToArray();
    }

    static void GetWavFilesRecursive(string folderPath, List<string> wavFiles)
    {
        try
        {
            wavFiles.AddRange(Directory.GetFiles(folderPath, "*.wav"));
            foreach (string directory in Directory.GetDirectories(folderPath))
            {
                GetWavFilesRecursive(directory, wavFiles);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Debug.LogError("Unauthorized access to folder: " + folderPath);
        }
    }
}