using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Studio.Sound;
using IllusionMods.Koikatsu3DSEModTools;

// This script generates prefabs from .wav files in a specified folder.

public class GeneratePrefabsFromSource : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate Only Prefabs")]
    public static void GeneratePrefabs()
    {
        GeneratePrefabs(true);
    }

    public static void GeneratePrefabs(bool create)
    {
        Debug.Log("Generate Prefabs started...");

        string selectedPath;
        bool isSidePanel;
        Utils.GetSelectedFolderPath(out selectedPath, out isSidePanel);

        Debug.Log("Select path: " + selectedPath);

        KK3DSEModManager modManager = new KK3DSEModManager(selectedPath, true);
        List<Category> categories = modManager.GetCategories();
        modManager.GeneratePrefabs(true, isSidePanel, categories);

        Debug.Log("Generate Prefabs completed for: " + selectedPath);
    }
}