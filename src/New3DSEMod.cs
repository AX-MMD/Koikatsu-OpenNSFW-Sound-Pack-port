using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml;
using IllusionMods.Koikatsu3DSEModTools;

public class New3DSEMod : MonoBehaviour
{
	[MenuItem("Assets/3DSE/New 3DSE Mod")]
	public static void CopyAndRename()
	{
		string sourcePath = "Assets/Examples/Studio 3DSE Example";
		string destinationPath = "Assets/Mods";

		if (!Directory.Exists(sourcePath))
		{
			Debug.LogError("Source path does not exist: " + sourcePath);
			return;
		}

		if (!Directory.Exists(destinationPath))
		{
			Directory.CreateDirectory(destinationPath);
		}

		New3DSEModWindow.ShowWindow(sourcePath, destinationPath);
	}

	public static void CopyDirectory(string sourceDir, string destDir)
	{
		Directory.CreateDirectory(destDir);

		foreach (string file in Directory.GetFiles(sourceDir))
		{
			string destFile = Path.Combine(destDir, Path.GetFileName(file));
			File.Copy(file, destFile);
		}

		foreach (string directory in Directory.GetDirectories(sourceDir))
		{
			string destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
			CopyDirectory(directory, destDirectory);
		}
	}

}

public class New3DSEModWindow : EditorWindow
{
	private static string sourcePath;
	private static string destinationPath;
	private Dictionary<string, string> fields;

	public New3DSEModWindow()
	{
		fields = new Dictionary<string, string>
		{
			{"name", ""},
			{"version", "1.0"},
			{"author", ""},
			{"description", ""},
			{"website", ""},
			{"itemGroupName", "3DSE"},
			{"muid", ""}
		};
	}

	public static void ShowWindow(string srcPath, string destPath)
	{
		sourcePath = srcPath;
		destinationPath = destPath;
		GetWindow<New3DSEModWindow>("New 3DSE Mod");
	}

	private void OnGUI()
	{
		GUILayout.Label("Your Mod name:", EditorStyles.boldLabel);
		fields["name"] = EditorGUILayout.TextField("Mod Name", fields["name"]);

		GUILayout.Label("Your Author name:", EditorStyles.boldLabel);
		fields["author"] = EditorGUILayout.TextField("Author", fields["author"]);

		GUILayout.Label("Tab name (leave default if you want it in the 3D SFX tab)", EditorStyles.boldLabel);
		fields["itemGroupName"] = EditorGUILayout.TextField("Item Group Name", fields["itemGroupName"]);

		GUILayout.Label("3-6 Digits unique ID:", EditorStyles.boldLabel);
		fields["muid"] = EditorGUILayout.TextField("Mod UID", fields["muid"]);

		GUILayout.Label("-------------------", EditorStyles.boldLabel);
		fields["version"] = EditorGUILayout.TextField("Version", fields["version"]);
		fields["description"] = EditorGUILayout.TextField("Description", fields["description"]);
		fields["website"] = EditorGUILayout.TextField("Website", fields["website"]);

        string newDestinationPath = "";

		if (GUILayout.Button("Create"))
		{
			try
			{
                fields["guid"] = Utils.MakeModGuid(fields["author"], fields["name"]);
				Utils.ManifestInfo manifest = new Utils.ManifestInfo(fields);
				List<string> errors = manifest.validate();

				if (errors.Count > 0)
				{
					throw new Exception(string.Join("\n", errors.ToArray()));
				}

				newDestinationPath = Path.Combine(destinationPath, manifest.name);
				if (sourcePath == newDestinationPath)
				{
					throw new Exception("Source and destination path must be different.");
				}

				New3DSEMod.CopyDirectory(sourcePath, newDestinationPath);
				manifest.save(Path.Combine(newDestinationPath, "manifest.xml"));

				string listPath = Path.Combine(newDestinationPath, Utils.GetItemDataFolder());
				List<string> csvLines = Utils.GetItemGroupHeaders();
				if (fields["itemGroupName"] == "3DSE")
				{
					csvLines.Add("11,3DSE" );
				}
				else
				{
					csvLines.Add(manifest.muid + "," + fields["itemGroupName"]);
				}
				Utils.WriteToCsv(Path.Combine(listPath, "ItemGroup_DataFiles.csv"), csvLines);

				// Rename the ItemCategory CSV file
				if (fields["itemGroupName"] != "3DSE")
				{
					File.Move(
						Path.Combine(listPath, "ItemCategory_00_11.csv"), 
						Path.Combine(listPath, "ItemCategory_00_" + manifest.muid + ".csv")
					);
				}

				// Rename the ItemList CSV file
				string oldCsvPath = Path.Combine(listPath, "ItemList_00_11_YYY.csv");
				if (fields["itemGroupName"] == "3DSE")
				{
					File.Move(
						oldCsvPath, 
						Path.Combine(listPath, "ItemList_00_11_" + manifest.muid + "01" + ".csv")
					);
				}
				else
				{
					File.Move(
						oldCsvPath, 
						Path.Combine(listPath, "ItemList_00_" + manifest.muid + "_YYY.csv")
					);
				}

				AssetDatabase.Refresh();
				Debug.Log("New 3DSE mod folder '" + manifest.name + "' created successfully.");
				this.Close();
			}
            catch (Exception e)
            {
                if (Directory.Exists(newDestinationPath))
                {
                    Directory.Delete(newDestinationPath, true);
                }
                throw e;
            }
		}
	}
}