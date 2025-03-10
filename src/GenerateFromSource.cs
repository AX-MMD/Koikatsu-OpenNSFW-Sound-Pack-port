using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using IllusionMods.Koikatsu3DSEModTools;
using IllusionMods.Koikatsu3DSECategoryTool;

public class GenerateFromSource : MonoBehaviour
{
	[MenuItem("Assets/3DSE/Generation Tools/Create From Source", true)]
	[MenuItem("Assets/3DSE/Generation Tools/Update From Source", true)]
	[MenuItem("Assets/3DSE/Generation Tools/Create CSV From Source", true)]
	[MenuItem("Assets/3DSE/Generation Tools/Update CSV From Source", true)]
	private static bool ValidateGenerate()
	{
		return Utils.GetSelected3DSEModPaths().Count > 0;
	}

	[MenuItem("Assets/3DSE/Generation Tools/Create From Source")]
	public static void Create(MenuCommand command)
	{
		Generate(true, false, command);
	}

	[MenuItem("Assets/3DSE/Generation Tools/Update From Source")]
	public static void Update(MenuCommand command)
	{
		Generate(false, false, command);
	}

	[MenuItem("Assets/3DSE/Generation Tools/Create CSV From Source")]
	public static void CreateCSV(MenuCommand command)
	{
		Generate(true, true, command);
	}

	[MenuItem("Assets/3DSE/Generation Tools/Update CSV From Source")]
	public static void UpdateCSV(MenuCommand command)
	{
		Generate(false, true, command);
	}


	// Main function to generate CSV and Prefabs
	private static void Generate(bool create, bool csvOnly, MenuCommand command)
	{
		string title = create ? "Create From Source" : "Update From Source";
		if (csvOnly)
		{
			title = "CSV " + title;
		}

		IEnumerable<string> selectedPaths = Utils.GetSelected3DSEModPaths();
		Utils.GenerationResult result = new Utils.GenerationResult {};

		if (create)
		{
			// If the /Prefab folder is not empty, show a warning dialog
			string[] modWarnings = selectedPaths
				.Where((string modPath) => Directory.GetFileSystemEntries(KK3DSEModManager.GetPrefabFolderPath(modPath)).Length > 0)
				.Select((string modPath) => string.Format("\n{0}/List/Studio/DataFiles" + (csvOnly ? "" : "\n{0}/Prefabs"), Path.GetFileName(modPath)))
				.ToArray();

			if (modWarnings.Count() > 0)
			{
				if ( ! EditorUtility.DisplayDialog(
					"Warning", 
					string.Format("'{0}' operation will rewrite the following folders based on /Sources:\n {1}\n\nContinue?", title, string.Join("", modWarnings)), 
					"Yes", "No"
				))
				{
					return;
				}
			}
		}

		foreach (string selectedPath in selectedPaths)
		{
			Debug.Log("Select path: " + selectedPath);
			KK3DSEModManager modManager = null;

			try
			{
				modManager = new KK3DSEModManager(selectedPath);
				List<Category> categories = new CategoryManager().BuildFromSource(modManager.sourcesPath);

				Utils.GenerationResult countA = modManager.GenerateCSV(create, categories);
				Debug.Log("CSV -> Created: " + countA.createCount + " Updated: " + countA.updateCount + " Deleted: " + countA.deleteCount);

				if (csvOnly)
				{
					result.createCount += countA.createCount;
					result.updateCount += countA.updateCount;
					result.deleteCount += countA.deleteCount;
					continue;
				}

				if (!create && countA.deleteCount <= countA.createCount && countA.createCount > 0)
				{
					// This case can happen when the user modifies his /Sources folder.
					// File/folder modification changes the prefab name, so they will be deleted and recreated.
					if ( ! EditorUtility.DisplayDialog(
						"Warning", 
						string.Format("'{0}' operation would delete {1} prefabs and create {2} prefabs based on /Sources.\n\nContinue?", title, countA.deleteCount, countA.createCount), 
						"Yes", "No"
					))
					{
						modManager.RestoreBackupCSV();
						continue;
					}
				}

				Utils.GenerationResult countB = modManager.GeneratePrefabs(create, categories);
				Debug.Log("Prefab -> Created: " + countB.createCount + " Updated: " + countB.updateCount + " Deleted: " + countB.deleteCount);

				result.createCount += countB.createCount;
				result.updateCount += countB.updateCount;
				result.deleteCount += countB.deleteCount;
				if (countA.createCount != countB.createCount)
				{
					Debug.LogWarning(string.Format("Mismatched create: {0} in CSV vs {1} prefabs", countA.createCount, countB.createCount));
				}
				if (countA.updateCount != countB.updateCount)
				{
					Debug.LogWarning(string.Format("Mismatched updates: {0} in CSV vs {1} prefabs", countA.updateCount, countB.updateCount));
				}
				if (countA.deleteCount != countB.deleteCount)
				{
					Debug.LogWarning(string.Format("Mismatched deletes: {0} in CSV vs {1} prefabs", countA.deleteCount, countB.deleteCount));
				}
			}
			catch (Exception e)
			{
				Utils.LogErrorWithTrace(e);
				EditorUtility.ClearProgressBar();
				if (modManager != null)
				{
					modManager.RestoreBackupCSV();
				}
				EditorUtility.DisplayDialog("Error", e.Message, "OK");
				return;
			}
			finally
			{
				if (modManager != null)
				{
					modManager.Dispose();
				}
			}
		}
		EditorUtility.ClearProgressBar();
		AssetDatabase.Refresh();
		Debug.Log("Created: " + result.createCount + " Updated: " + result.updateCount + " Deleted: " + result.deleteCount);
		EditorUtility.DisplayDialog("Success", title + " completed.\n\n" + "Created: " + result.createCount + "\nUpdated: " + result.updateCount + "\nDeleted: " + result.deleteCount, "OK");
	}
}