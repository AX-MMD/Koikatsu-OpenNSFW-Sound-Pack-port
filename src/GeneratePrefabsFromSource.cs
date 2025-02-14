using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Studio.Sound;

// This script generates prefabs from .wav files in a specified folder.

public class GeneratePrefabsFromSource : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate Only Prefabs")]
    public static void GeneratePrefabs()
    {
        GeneratePrefabs(false);
    }

    public static void GeneratePrefabs(bool create)
    {
        Debug.Log("Generate Prefabs From Source started...");
        string selectedPath;
		bool isSidePanel;
		Utils.GetSelectedFolderPath(out selectedPath, out isSidePanel);

        string modPath = Utils.GetModPath(selectedPath);
        string outputPath = Path.Combine(modPath, "Prefab");
        string basePrefabPath = Path.Combine(modPath, "base_3dse.prefab");
        string sourcesPath;
        if (isSidePanel)
        {
            sourcesPath = Path.Combine(modPath, "Sources");
        }
        else
        {
            sourcesPath = selectedPath;
        }

        Debug.Log("Starting prefab generation...");
        Debug.Log("Select path: " + selectedPath);
        Debug.Log("Source path: " + sourcesPath);
        Debug.Log("Output path: " + outputPath);
        Debug.Log("Base prefab path: " + basePrefabPath);

        if (!Directory.Exists(sourcesPath))
        {
            throw new Exception("Source path does not exist: " + sourcesPath);
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            Debug.Log("Created output directory: " + outputPath);
        }
        else if (create && isSidePanel)
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
            throw new Exception("Base prefab not found at path: " + basePrefabPath);
        }

        string[] wavFiles = GetAllWavFiles(sourcesPath);
        string folderName = Path.GetFileName(sourcesPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        string modName = Utils.GetModName(selectedPath);
        string basePath = "studio/" + modName;

        int i = 1;

        foreach (string wavFile in wavFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(wavFile);
            string itemName = GetItemName(fileName, folderName);
            string newPrefabName = string.Format("{0}.prefab", itemName);
            string newPrefabPath = Path.Combine(outputPath, newPrefabName).Replace("\\", "/");

            GameObject newObject = null;

            if (!create && File.Exists(newPrefabPath))
            {
                //update the existing prefab
                Debug.Log("Updating prefab: " + newPrefabPath);
                newObject = (GameObject)AssetDatabase.LoadAssetAtPath(newPrefabPath, typeof(GameObject));
            }
            else
            {
                //create a new prefab, if there is already a prefab with the same name, it will be overwritten
                Debug.Log("Creating prefab: " + newPrefabPath);
                newObject = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);                
            }

            // Load the AudioClip from the .wav file
            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavFile);
            if (audioClip == null)
            {
                throw new Exception("AudioClip not found at path: " + wavFile);
            }

            // Assign the AudioClip to the SEComponent
            SEComponent seComponent = newObject.GetComponent<SEComponent>();
            if (seComponent != null)
            {
                seComponent._clip = audioClip;
            }
            else
            {
                throw new Exception("SEComponent not found on the instantiated prefab.");
            }

            // Create the prefab, overwrite if it already exists            
            PrefabUtility.CreatePrefab(newPrefabPath, newObject);
            DestroyImmediate(newObject);

            // Set the asset bundle name in the .meta file
            string metaFilePath = newPrefabPath + ".meta";
            if (File.Exists(metaFilePath))
            {
                string relativePath = Utils.GetRelativePath(sourcesPath, Path.GetDirectoryName(wavFile));
                // split the relative path into parts, snake case them, and join them with slashes
                relativePath = string.Join("/", Array.ConvertAll(relativePath.Split(Path.DirectorySeparatorChar), Utils.ToSnakeCase));
                string bundlePath = string.IsNullOrEmpty(relativePath) ? basePath + "/bundle.unity3d" : basePath + "/" + relativePath + "bundle.unity3d";
                Utils.SetAssetBundleNameInMetaFile(metaFilePath, bundlePath);
            }
            else
            {
                throw new Exception("Meta file not found for prefab: " + newPrefabPath);
            }

            Debug.Log("Prefab created: " + newPrefabPath);

            i++;
        }

        AssetDatabase.Refresh();
        Debug.Log(string.Format("{0} Prefabs generated successfully.", i - 1));
    }

    private static string GetItemName(string fileName, string folderName)
    {
        return Utils.ToSnakeCase(fileName);
    }

    private static string[] GetAllWavFiles(string sourcesPath)
    {
        List<string> wavFiles = new List<string>();
        GetWavFilesRecursive(sourcesPath, wavFiles);
        return wavFiles.ToArray();
    }

    private static void GetWavFilesRecursive(string sourcesPath, List<string> wavFiles)
    {
        wavFiles.AddRange(Directory.GetFiles(sourcesPath, "*.wav"));
        foreach (string directory in Directory.GetDirectories(sourcesPath))
        {
            GetWavFilesRecursive(directory, wavFiles);
        }
    }
}