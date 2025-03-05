using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Studio.Sound;
using IllusionMods.Koikatsu3DSEModTools;

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
				int countB = modManager.GeneratePrefabs(true, isSidePanel, categories);
				Debug.Log("Generated " + countB + " items for " + selectedPath);
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
		EditorUtility.DisplayDialog("Success", "Generated " + total + " prefabs.", "OK");
		AssetDatabase.Refresh();
    }
}