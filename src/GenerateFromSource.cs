using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using IllusionMods.Koikatsu3DSEModTools;

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

		KK3DSEModManager modManager = new KK3DSEModManager(selectedPath, true);
		List<Category> categories = modManager.GetCategories();
		Debug.Log("Categories: " + categories.Count);
		modManager.GenerateCSV(true, isSidePanel, categories);
		modManager.GeneratePrefabs (true, isSidePanel, categories);

		Debug.Log("Generate From Source completed for: " + selectedPath);
	}
}