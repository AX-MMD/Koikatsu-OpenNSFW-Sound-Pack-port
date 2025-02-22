using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Studio.Sound;

public static class Utils
{
    public static void GetSelectedFolderPath(out string selectedPath, out bool isSidePanel)
    {
        if (Selection.assetGUIDs.Length > 0)
        {
            selectedPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[Selection.assetGUIDs.Length - 1]);
            isSidePanel = true;
        }
        else if (Selection.activeObject != null)
        {
            selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            isSidePanel = false;
        }
        else
        {
            throw new Exception("Please select a folder in the Assets/Mods directory.");
        }

        if (string.IsNullOrEmpty(selectedPath))
        {
            throw new Exception("Selection cannot be empty");
        }

        if (isSidePanel)
        {
            string parentFolder = Path.GetFileName(Path.GetDirectoryName(selectedPath));
            if (!Directory.Exists(selectedPath) || parentFolder != "Mods")
            {
                Debug.LogError("Selection parent folder is '" + parentFolder + "' while it should be 'Mods'");
                throw new Exception("Invalid side panel folder '" + selectedPath + "', select Mods/<your_mod_name> or an individual file/folder in Sources");
            }
        }
        else if (!File.Exists(selectedPath) && !Directory.Exists(selectedPath))
        {
            throw new Exception("Selection does not exist: " + selectedPath);
        }
    }

    public static string GetModPath(string selectedPath)
    {
        string[] directories = selectedPath.Split('/');
        Debug.Log("Directories: " + string.Join(", ", directories));
        for (int i = 0; i < directories.Length; i++)
        {
            if (directories[i] == "Mods" && i + 1 < directories.Length)
            {
                return string.Join(Path.DirectorySeparatorChar.ToString(), directories, 0, i + 2);
            }
        }

        throw new Exception("Mods folder not found in path: " + selectedPath);
    }

    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string snakeCase = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower();
        snakeCase = Regex.Replace(snakeCase, @"[\s!@#$%^&*()\-_=+\[\]{};:'"",<.>/?\\|~`]", "_");
        snakeCase = Regex.Replace(snakeCase, @"_+", "_");
        return snakeCase;
    }

    public static string GetModName(string selectedPath)
    {
        string studioPath = Path.Combine(GetModPath(selectedPath), "List/Studio");
        string[] directories = Directory.GetDirectories(studioPath);
        if (directories.Length == 0)
        {
            throw new Exception("No directories found in List/Studio.");
        }

        return Path.GetFileName(directories[0]);
    }

    public static string GetModCsvPath(string modPath, string modName)
    {
        string[] listsPath = Directory.GetDirectories(Path.Combine(modPath, "List/Studio"));
        if (listsPath.Length == 0)
        {
            throw new Exception("Missing List/Studio/<mod_folder> folder in mod directory: " + modPath);
        }
        else if (listsPath.Length > 1)
        {
            throw new Exception("Multiple List/Studio folders found in mod directory: " + modPath);
        }
        else
        {
            return listsPath[0];
        }
    }

    public static string GetRelativePath(string basePath, string fullPath)
    {
        basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        fullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        Uri baseUri = new Uri(basePath);
        Uri fullUri = new Uri(fullPath);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }

    public static void SetAssetBundleNameInMetaFile(string metaFilePath, string bundleName)
    {
        string[] lines = File.ReadAllLines(metaFilePath);
        bool found = false;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("  assetBundleName:"))
            {
                lines[i] = "  assetBundleName: " + bundleName;
                found = true;
                break;
            }
        }

        if (!found)
        {
            List<string> linesList = new List<string>(lines);
            linesList.Insert(linesList.Count - 1, "  assetBundleName: " + bundleName);
            lines = linesList.ToArray();
        }

        File.WriteAllLines(metaFilePath, lines);
    }

    public static SEComponent GetSEComponent(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new Exception("Prefab not found at path: " + prefabPath);
        }
        else
        {
            return prefab.GetComponent<SEComponent>();
        }

    }

    public static bool IsPrefabAudioLoop(string prefabPath)
    {
        SEComponent seComponent = GetSEComponent(prefabPath);
        if (seComponent == null)
        {
            throw new Exception("Prefab does not have an SEComponent: " + prefabPath);
        }
        else{
            return seComponent._isLoop == true;
        }
    }
}