using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;

public class RenameWavsFromParentName : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Rename Wavs From Parent Name")]
    public static void RenameWavFiles()
    {
        string folderPath = GetSelectedFolderPaths();
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Please select a folder in the Assets directory.");
            return;
        }

        Debug.Log("Starting renaming of WAV files...");
        Debug.Log("Source path: " + folderPath);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Folder path does not exist: " + folderPath);
            return;
        }

        string[] wavFiles = Directory.GetFiles(folderPath, "*.wav");
        string folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string folderNameSnakeCase = ToSnakeCase(folderName);

        int i = 1;

        foreach (string wavFile in wavFiles)
        {
            string newFileName = string.Format("{0}{1:00}.wav", folderNameSnakeCase, i);
            string newFilePath = Path.Combine(folderPath, newFileName);

            Debug.Log("Renaming file: " + wavFile + " to " + newFilePath);

            File.Move(wavFile, newFilePath);

            Debug.Log("File renamed: " + newFilePath);
        }

        AssetDatabase.Refresh();
        Debug.Log(string.Format("{0} WAV files renamed successfully.", i - 1));
    }

    private static string GetSelectedFolderPaths()
    {
        string path = null;
        if (Selection.activeObject != null)
        {
            path = AssetDatabase.GetAssetPath(Selection.activeObject);
        }
        else if (Selection.assetGUIDs.Length > 0)
        {
            path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
        }
        else
        {
            Debug.LogError("Selection is empty");
            return null;
        }

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
        Regex.Replace(snakeCase, @"[\s!@#$%^&()\-=+\[\]{};:',~`]", "_");
        snakeCase = Regex.Replace(snakeCase, @"_+", "_");
        return snakeCase;
    }
}