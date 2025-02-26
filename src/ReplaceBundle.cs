using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Studio.Sound;

public class ModifyAndRebundlePrefabs : MonoBehaviour
{
	[MenuItem("Assets/3DSE/Modify and Rebundle Prefabs in Folder")]
	public static void ModifyAndRebundleInFolder()
	{
		string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
		if (string.IsNullOrEmpty(folderPath))
		{
			Debug.LogError("No folder selected.");
			return;
		}

		// Process all .unity3d files in the folder and subfolders
		string[] bundlePaths = Directory.GetFiles(folderPath, "*.unity3d", SearchOption.AllDirectories);
		foreach (string bundlePath in bundlePaths)
		{
			if (!bundlePath.Contains("unity3d"))
			{
				continue;
			}
			ModifyAndRebundle(folderPath, bundlePath);
		}

		Debug.Log("All prefabs modified and re-bundled successfully.");
	}

	private static void ModifyAndRebundle(string folderPath, string bundlePath)
	{
		// Load the bundle
		AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
		if (bundle == null)
		{
			Debug.LogError("Failed to load AssetBundle: " + bundlePath);
			return;
		}

		// Extract all prefabs from the bundle
		string[] assetNames = bundle.GetAllAssetNames();
		List<GameObject> prefabs = new List<GameObject>();
		foreach (string assetName in assetNames)
		{
			GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
			if (prefab != null)
			{
				prefabs.Add(prefab);
			}
		}

		// Modify the _rolloffDistance of the SEComponent
		foreach (GameObject prefab in prefabs)
		{
			SEComponent seComponent = prefab.GetComponent<SEComponent>();
			if (seComponent != null)
			{
				seComponent._rolloffDistance.min = 1f;
				seComponent._rolloffDistance.max = 5f;
				EditorUtility.SetDirty(prefab);
			}
		}

		// Save the modified prefabs to a temporary folder within the Assets directory
		string tempFolder = "Assets/TempPrefabs";
		if (Directory.Exists(tempFolder))
		{
			Directory.Delete(tempFolder, true);
		}
		if (!Directory.Exists(tempFolder))
		{
			Directory.CreateDirectory(tempFolder);
		}

		List<string> tempPrefabPaths = new List<string>();
		foreach (GameObject prefab in prefabs)
		{
			string tempPrefabPath = Path.Combine(tempFolder, prefab.name + ".prefab").Replace("\\", "/");
			PrefabUtility.CreatePrefab(tempPrefabPath, prefab);
			tempPrefabPaths.Add(tempPrefabPath);
		}

		// Create a new AssetBundle with the modified prefabs
		string bundleName = Path.GetFileName(bundlePath);
		BuildPipeline.BuildAssetBundles(Path.GetDirectoryName(bundlePath), new AssetBundleBuild[]
			{
				new AssetBundleBuild
				{
					assetBundleName = bundleName,
					assetNames = tempPrefabPaths.ToArray()
				}
			}, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);

		// Clean up temporary files
		foreach (string tempPrefabPath in tempPrefabPaths)
		{
			AssetDatabase.DeleteAsset(tempPrefabPath);
		}
		Directory.Delete(tempFolder);

		// Unload the bundle
		bundle.Unload(false);

		Debug.Log("Prefabs modified and re-bundled successfully: " + bundlePath);
	}
}