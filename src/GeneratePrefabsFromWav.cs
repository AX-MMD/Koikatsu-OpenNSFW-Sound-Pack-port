using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using Studio.Sound;


// This script generates prefabs from .wav files in a specified folder.

public class GeneratePrefabsFromWav : MonoBehaviour
{
    [MenuItem("Assets/Generate Prefabs From WAV")]
    public static void GeneratePrefabs()
    {
		string folderPath = "Assets/3DSE Sources/OpenNSFW/Lewd K. Sound/LiteRoomyPlaps"; // Replace with your folder path
        string outputPath = "Assets/Mods/OpenNSFW port - Plaps/Prefab"; // Replace spaces with underscores
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

        Object basePrefab = AssetDatabase.LoadAssetAtPath(basePrefabPath, typeof(GameObject));
        if (basePrefab == null)
        {
            Debug.LogError("Base prefab not found at path: " + basePrefabPath);
            return;
        }

        string[] wavFiles = Directory.GetFiles(folderPath, "*.wav");
        string folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string snakeCaseFolderName = ToSnakeCase(folderName);
        int i = 1;

        foreach (string wavFile in wavFiles)
        {
            string itemName = GetItemName(Path.GetFileNameWithoutExtension(wavFile));
            string newPrefabName = string.Format("{0}{1}.prefab", snakeCaseFolderName, itemName);
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

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string snakeCase = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower();
        snakeCase = Regex.Replace(snakeCase, @"\s+", "_");
        return snakeCase;
    }

    private static string GetItemName(string fileName)
    {
        // Use an appropriate regex pattern to extract numbers from the file names
		//Match match = Regex.Match(fileName, @"^(.*)\s*-\s*\((\d+)\)$");
		Match match = Regex.Match(fileName, @"^(.*)_(\d+)$");
        if (match.Success)
        {
            return match.Groups[2].Value;
        }
        return fileName;
    }
}