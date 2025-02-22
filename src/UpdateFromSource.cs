using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using KK3DSEModManager;
using Models;

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
        List<Category> categories = modManager.getCategories();
        modManager.generatePrefabs(false, isSidePanel, categories);
        modManager.generateCSV(false, isSidePanel, categories);

        Debug.Log("Update From Source completed successfully.");
    }
}