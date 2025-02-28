using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using IllusionMods.Koikatsu3DSEModTools;

public class UpdateFromSource : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Update From Source")]
    public static void RegenAll()
    {
        Debug.Log("Update From Source started...");
        
        string selectedPath;
        bool isSidePanel;
        Utils.GetSelectedFolderPath(out selectedPath, out isSidePanel);

        KK3DSEModManager modManager = new KK3DSEModManager(selectedPath);
        List<Category> categories = modManager.GetCategories();
        modManager.GeneratePrefabs(false, isSidePanel, categories);
        modManager.GenerateCSV(false, isSidePanel, categories);

        Debug.Log("Update From Source completed successfully.");
    }
}