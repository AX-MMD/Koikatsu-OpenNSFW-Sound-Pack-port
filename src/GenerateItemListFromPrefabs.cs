using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using IllusionMods.Koikatsu3DSEModTools;

public class GenerateItemFiles : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate Only Item Files")]
    public static void GenerateCSV()
    {
        GenerateCSV(true);
    }

    public static void GenerateCSV(bool create)
    {
        Debug.Log("Generate Item Files started...");
        int total = 0;
        string[] selectedPaths;
		bool isSidePanel;
		Utils.GetSelectedFolderPaths(out selectedPaths, out isSidePanel);

		foreach (string selectedPath in selectedPaths)
		{
			Debug.Log("Select path: " + selectedPath);
            try
            {
                KK3DSEModManager modManager = new KK3DSEModManager(selectedPath, true);
                List<Category> categories = modManager.GetCategories();
                total += modManager.GenerateCSV(true, isSidePanel, categories);
            }
            catch (Exception e)
            {
                Utils.LogErrorWithTrace(e);
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
                return;
            }
		}
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Success", "Generated Item files completed for " + total + " items.", "OK");
		AssetDatabase.Refresh();
    }
}