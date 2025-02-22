using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class GenerateFromSource : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate From Source")]
    public static void RegenAll()
    {
        Debug.Log("Generate From Source started...");
        
        string selectedPath;
        bool isSidePanel;
        Utils.GetSelectedFolderPath(out selectedPath, out isSidePanel);

        Debug.Log("Select path: " + selectedPath);

        KK3DSEModManager modManager = new KK3DSEModManager(selectedPath);
        List<Category> categories = modManager.getCategories();
        modManager.generateCSV(true, isSidePanel, categories);
        modManager.generatePrefabs(true, isSidePanel, categories)

        Debug.Log("Generate From Source completed successfully.");
    }
}