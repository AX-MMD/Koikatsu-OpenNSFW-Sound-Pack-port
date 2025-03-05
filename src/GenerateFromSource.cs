using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using IllusionMods.Koikatsu3DSEModTools;

public class GenerateFromSource : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate From Source", true)]
    [MenuItem("Assets/3DSE/Update From Source", true)]
    private static bool ValidateRegenAll()
    {
        if (Selection.objects.Length == 0 && Selection.assetGUIDs.Length > 0)
        {
            // Check if the objects from Selection.assetGUIDs have Assets/Mods as parent
            foreach (string guid in Selection.assetGUIDs)
            {
                if (!Utils.IsValid3DSEModPath(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    return false;
                }
            }
            return true;
        }
		else if (Selection.activeObject != null)
		{
			string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			return AssetDatabase.IsValidFolder(selectedPath) && Utils.IsValid3DSEModPath(Utils.GetModPath(selectedPath));
		}
        else
        {
            return false;
        }
    }

    [MenuItem("Assets/3DSE/Generate From Source")]
    public static void RegenAllGenerate(MenuCommand command)
    {
        RegenAll(false, command);
    }

    [MenuItem("Assets/3DSE/Update From Source")]
    public static void RegenAllUpdate(MenuCommand command)
    {
        RegenAll(true, command);
    }

    private static void RegenAll(bool create, MenuCommand command)
    {
        string title = create ? "Update From Source" : "Generate From Source";
        Debug.Log(title + " started...");

        int total = 0;
        bool isSidePanel = true;
        List<string> selectedPaths = new List<string>();
        foreach (string guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Utils.IsValid3DSEModPath(path) && !Selection.objects.Any(obj => AssetDatabase.GetAssetPath(obj) == path))
            {
                selectedPaths.Add(path);
            }
        }

        foreach (string selectedPath in selectedPaths)
        {
            Debug.Log("Select path: " + selectedPath);
            try
            {
                KK3DSEModManager modManager = new KK3DSEModManager(selectedPath, true);
                List<Category> categories = modManager.GetCategories();
                int countA = modManager.GenerateCSV(create, isSidePanel, categories);
                int countB = modManager.GeneratePrefabs(create, isSidePanel, categories);
                Debug.Log("Generated " + countB + " items for " + selectedPath);
                total += countB;
                if (countA != countB)
                {
                    Debug.LogWarning(string.Format("Mismatched item count: {0} items for {1} prefabs", countA, countB));
                }
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
        EditorUtility.DisplayDialog("Success", title + " completed for " + total + " items.", "OK");
        AssetDatabase.Refresh();
    }
}