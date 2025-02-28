using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using IllusionMods.Koikatsu3DSEModTools;

public class GenerateItemListFromPrefabs : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate Only ItemList")]
    public static void GenerateCSV()
    {
        GenerateCSV(true);
    }

    public static void GenerateCSV(bool create)
    {
        Debug.Log("Generate ItemList started...");
        string selectedPath;
        bool isSidePanel;
        Utils.GetSelectedFolderPath(out selectedPath, out isSidePanel);

        Debug.Log("Select path: " + selectedPath);

        KK3DSEModManager modManager = new KK3DSEModManager(selectedPath, true);
        List<Category> categories = modManager.GetCategories();
        modManager.GenerateCSV(true, isSidePanel, categories);

        Debug.Log("Generate ItemList completed for: " + selectedPath);
    }
}