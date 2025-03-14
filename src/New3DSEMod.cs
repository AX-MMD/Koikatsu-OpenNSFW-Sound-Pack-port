using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml;
using IllusionMods.Koikatsu3DSEModTools;
using IllusionMods.KoikatsuStudioCsv;

public class New3DSEMod : MonoBehaviour
{
	[MenuItem("Assets/3DSE/Edit 3DSE Mod", true)]
	public static bool ValidateEdit3DSEMod()
	{
		return Utils.GetSelected3DSEModPaths().Count == 1;
	}

	[MenuItem("Assets/3DSE/New 3DSE Mod")]
	public static void MakeNew3DSEMod(MenuCommand command)
	{
		Modify3DSEMod(true, command);
	}

	[MenuItem("Assets/3DSE/Edit 3DSE Mod")]
	public static void Edit3DSEMod(MenuCommand command)
	{
		Modify3DSEMod(false, command);
	}

	public static void Modify3DSEMod(bool create, MenuCommand command)
	{
		string sourcePath;
		string destinationPath;
		if (create)
		{
			sourcePath = "Assets/Examples/Studio 3DSE Example";
			destinationPath = "Assets/Mods";
			if (!Directory.Exists(sourcePath))
			{
				EditorUtility.DisplayDialog("Error", "Mod template path does not exist: " + sourcePath, "OK");
				return;
			}

			if (!Directory.Exists(destinationPath))
			{
				Directory.CreateDirectory(destinationPath);
			}
		}
		else
		{
			sourcePath = Utils.GetSelected3DSEModPaths().First();
			destinationPath = sourcePath;
		}

		Modify3DSEModWindow.ShowWindow(sourcePath, destinationPath, create);
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

public class Modify3DSEModWindow : EditorWindow
{
	private static string sourcePath;
	private static string destinationPath;
	private static bool createMode;
	private static string itemGroupName = "3DSE";
	private static string oldItemGroupName = null;
	private static CsvUtils.ItemFileAggregate itemFileAgg;
	private static Utils.ManifestInfo fields;
	private static Utils.ManifestInfo oldFields;

	public Modify3DSEModWindow()
	{
		fields = new Utils.ManifestInfo();
	}

	public static void ShowWindow(string srcPath, string destPath, bool create)
	{
		sourcePath = srcPath;
		destinationPath = destPath;
		createMode = create;
		if (create)
		{
			GetWindow<Modify3DSEModWindow>("New 3DSE Mod");
		}
		else
		{
			LoadForEdit(sourcePath);
			GetWindow<Modify3DSEModWindow>("Edit 3DSE Mod");
		}
	}

	private static void LoadForEdit(string path)
	{
		oldFields = new Utils.ManifestInfo(Path.Combine(path, "manifest.xml"));
		fields.Update(oldFields);
		itemFileAgg = CsvUtils.GetItemFileAggregate(path);

		EnsureDataFilesFolderIntegrity(itemFileAgg);

		if (itemFileAgg.groupFiles.Length != 1)
		{
			throw new Exception("Exactly 1 ItemGroup_*.csv file is expected in List/Studio folder " + path);
		}

		CsvUtils.StudioGroup group = itemFileAgg.GetFirstEntry<CsvUtils.StudioGroup>();
		if (group == null)
		{
			Debug.LogWarning("Empty ItemGroup file: " + itemFileAgg.groupFiles[0]);
			oldItemGroupName = itemGroupName = "";
		}
		else
		{
			oldItemGroupName = itemGroupName = group.name;
		}
	}

	private static void EnsureDataFilesFolderIntegrity(CsvUtils.ItemFileAggregate itemFileAgg)
	{
		if ( ! (new string[] { itemFileAgg.GetDefaultGroupFile(), itemFileAgg.GetDefaultCategoryFile(), itemFileAgg.GetDefaultListFile() }.Contains(null)))
		{
			return;
		}

		if ( ! EditorUtility.DisplayDialog("Rebuild Data Files", "Some List/Studio files are missing or corrupt, try to rebuild?", "Yes", "No"))
		{
			throw new Exception("Rebuild aborted by user");
		}

		Utils.Tuple<string> modInfo = itemFileAgg.GetModInfo();
		string groupNumber = modInfo.Item1;
		string categoryNumber = modInfo.Item2;

		// If groupNumber is null it means both ItemCategory and ItemList files are missing, rebuild assuming default (11, 3DSE).

		// ItemGroup integrity
		if (itemFileAgg.GetDefaultGroupFile() == null)
		{
			if (groupNumber == "11" || groupNumber == null)
			{
				CsvUtils.WriteToCsv(
					Path.Combine(itemFileAgg.csvFolder, "ItemGroup_" + Path.GetFileName(itemFileAgg.csvFolder) + ".csv"),
					new CsvUtils.StudioGroup[] { new CsvUtils.StudioGroup("11", "3DSE") }
				);
			}
			else 
			{
				File.Create(Path.Combine(itemFileAgg.csvFolder, "ItemGroup_" + Path.GetFileName(itemFileAgg.csvFolder) + ".csv")).Close();
			}
		}
		else if (groupNumber == null)
		{
			CsvUtils.StudioGroup first = itemFileAgg.GetFirstEntry<CsvUtils.StudioGroup>();
			groupNumber = first == null ? "11" : first.groupNumber;
		}

		// ItemCategory integrity
		if (itemFileAgg.GetDefaultCategoryFile() == null)
		{
			File.Create(Path.Combine(itemFileAgg.csvFolder, "ItemCategory_00_" + groupNumber + ".csv")).Close();
		}
		else if (categoryNumber == null)
		{
			CsvUtils.StudioCategory first = itemFileAgg.GetFirstEntry<CsvUtils.StudioCategory>();
			if (groupNumber == "11")
			{
				if (first != null)
				{
					categoryNumber = first.categoryNumber;
				}
				else if (!string.IsNullOrEmpty(fields.muid))
				{
					categoryNumber = fields.muid + "01";
				}
				else
				{
					throw new Exception("Rebuild Failed, too many missing elements");
				}
			}
			else
			{
				categoryNumber = first == null ? "01" : first.categoryNumber;
			}
		}

		// ItemList integrity
		if (null == itemFileAgg.GetDefaultListFile())
		{
			File.Create(Path.Combine(itemFileAgg.csvFolder, "ItemList_00_" + groupNumber + "_" + categoryNumber + ".csv")).Close();
		}

		itemFileAgg.Refresh();
	}

	private void OnGUI()
	{
		if (!createMode)
		{
			GUILayout.Label("Mod GUID", EditorStyles.boldLabel);
			fields.guid = EditorGUILayout.TextField("GUID", fields.guid);
		}
		GUILayout.Label("Mod name", EditorStyles.boldLabel);
		fields.name = EditorGUILayout.TextField("Mod Name", fields.name);

		GUILayout.Label("Author name", EditorStyles.boldLabel);
		fields.author = EditorGUILayout.TextField("Author", fields.author);

		GUILayout.Label("Studio Item Tab (default is '3D SFX' tab)", EditorStyles.boldLabel);
		itemGroupName = EditorGUILayout.TextField("Tab Name", itemGroupName);

		GUILayout.Label("3-6 Digits unique ID:", EditorStyles.boldLabel);
		fields.muid = EditorGUILayout.TextField("Mod UID", fields.muid);

		GUILayout.Label("-------------------", EditorStyles.boldLabel);
		fields.version = EditorGUILayout.TextField("Version", fields.version);
		fields.description = EditorGUILayout.TextField("Description", fields.description);
		fields.website = EditorGUILayout.TextField("Website", fields.website);

		if (createMode)
		{
			fields.guid = Utils.MakeModGuid(fields.author, fields.name);
		}

		if (GUILayout.Button(createMode ? "Create" : "Save") && (createMode || IsChanged()) && ValidateFields())
		{
			try
			{
				if (createMode)
				{
					CreateMod();
				}
				else
				{
					EditMod();
				}
			}
			catch (Exception e)
			{
				Utils.LogErrorWithTrace(e);
				EditorUtility.DisplayDialog("Error", e.Message, "OK");
			}
		}
	}

	private bool IsChanged()
	{
		return new Utils.ManifestInfo(Path.Combine(sourcePath, "manifest.xml")) != fields || itemGroupName != oldItemGroupName;
	}

	private bool ValidateFields()
	{
		List<string> errors = fields.Validate();

		if (itemGroupName == "")
		{
			errors.Add("Studio Item Tab is required.");
		}

		if (errors.Count > 0)
		{
			throw new Exception(string.Join("\n", errors.ToArray()));
		}

		return true;
	}

	private void CreateMod()
	{
		// Good luck //

		// Koikastu Studio CSV files convention used here:
		// * ItemGroup_<name_of_parent_folder>.csv
		// * ItemCategory_<index>_<first_group_in_ItemGroup>.csv
		// * ItemList_<index>_<first_group_in_ItemGroup>_<first_category_in_ItemCategory>.csv

		// <index> is not really relevent unless using multiple ItemXXX files, which is not the case here.

		// Case #1 itemGroupName is "3DSE", the categories will appear in Add -> Items -> 3D SFX and must have Category Numbers unique to other mods.
		// I use MUID + 01 as starting point for the Category Number.

		// MUID is a 3-6 digit ID that the user must choose, there is no garanty that it not already used by another Studio item mod.
		// The MUID is saved to the manifest.xml file. It is not a standard Koikatsu field.

		// Case #2 itemGroupName is not "3DSE" but <mod_name>, the categories wil appear in Add -> Items -> <group_name_in_ItemGroup> (same as <mod_name>).
		// The Group Number(s) must be unique to other mods in Add -> Items, but the categories only need to be unique within the group.
		// Typically only 1 entry in ItemGroup per mod, but it is not technically a requirement.
		// The MUID provided by the user is used as the Group Number.

		string newDestinationPath = "";

		try
		{
			newDestinationPath = Path.Combine(destinationPath, fields.name);
			if (sourcePath == newDestinationPath)
			{
				throw new Exception("Source and destination path must be different.");
			}

			New3DSEMod.CopyDirectory(sourcePath, newDestinationPath);
			fields.Save(Path.Combine(newDestinationPath, "manifest.xml"));

			string listPath = CsvUtils.GetItemDataFolder(newDestinationPath);
			var group = new List<CsvUtils.StudioGroup>();

			if (itemGroupName == "3DSE")
			{
				group.Add(new CsvUtils.StudioGroup("11", "3DSE" ));
				Utils.FileMove(
					Path.Combine(listPath, "ItemList_00_11_01.csv"), 
					Path.Combine(listPath, "ItemList_00_11_" + fields.muid + "01" + ".csv")
				);
			}
			else
			{
				group.Add(new CsvUtils.StudioGroup(fields.muid, itemGroupName));
				Utils.FileMove(
					Path.Combine(listPath, "ItemCategory_00_11.csv"), 
					Path.Combine(listPath, "ItemCategory_00_" + fields.muid + ".csv")
				);
				Utils.FileMove(
					Path.Combine(listPath, "ItemList_00_11_01.csv"),
					Path.Combine(listPath, "ItemList_00_" + fields.muid + "_01.csv")
				);
			}

			CsvUtils.WriteToCsv(Path.Combine(listPath, "ItemGroup_DataFiles.csv"), group);
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("Success", "Mod created successfully.", "OK");
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

	private void EditMod()
	{
		try
		{
			if (itemGroupName != oldItemGroupName)
			{
				List<CsvUtils.StudioGroup> groups = CsvUtils.DeserializeCsvStudio<CsvUtils.StudioGroup>(itemFileAgg.GetDefaultGroupFile());
				if (groups.Count == 0)
				{
					groups.Add(new CsvUtils.StudioGroup(itemGroupName == "3DSE" ? "11" : fields.muid, itemGroupName));
				}
				else
				{
					groups[0].name = itemGroupName;
				}
				CsvUtils.WriteToCsv(itemFileAgg.GetDefaultGroupFile(), groups);

				string categoryPath = itemFileAgg.GetDefaultCategoryFile();
				string listPath = itemFileAgg.GetDefaultListFile();

				Func<string, string> LambdaReplace;
				if (oldItemGroupName == "3DSE")
				{
					LambdaReplace = (string oldName) => Regex.Replace(oldName, "^" + oldFields.muid + "(\\d+)$", "$1");
					UpdateCategories(categoryPath, LambdaReplace);
					UpdateItems(itemFileAgg.GetDefaultListFile(), LambdaReplace);
					Utils.FileMove(
						categoryPath,
						Regex.Replace(categoryPath, "ItemCategory_(\\d+)_11.csv$", "ItemCategory_$1_" + fields.muid + ".csv")
					);
					Utils.FileMove(
						listPath,
						Regex.Replace(categoryPath, "ItemList_(\\d+)_11_" + oldFields.muid + "(\\d+).csv$", "ItemList_$1_" + fields.muid + "_$2.csv")
					);
				}
				else if (itemGroupName == "3DSE")
				{
					LambdaReplace = (string oldName) => Regex.Replace(oldName, "^(\\d+)$", fields.muid + "$1");
					UpdateCategories(categoryPath, LambdaReplace);
					UpdateItems(itemFileAgg.GetDefaultListFile(), LambdaReplace);
					Utils.FileMove(
						categoryPath,
						Regex.Replace(categoryPath, "ItemCategory_(\\d+)_" + oldFields.muid + ".csv$", "ItemCategory_$1_11.csv")
					);
					Utils.FileMove(
						listPath,
						Regex.Replace(categoryPath, "ItemList_(\\d+)_" + oldFields.muid + "_(\\d+).csv$", "ItemList_$1_11_" + fields.muid + "$2.csv")
					);
				}
				else
				{
					UpdateItems(itemFileAgg.GetDefaultListFile(), (string oldName) => oldName);
					Utils.FileMove(
						categoryPath,
						Regex.Replace(categoryPath, "ItemCategory_(\\d+)_" + oldFields.muid + ".csv$", "ItemCategory_$1_" + fields.muid + ".csv")
					);
					Utils.FileMove(
						listPath,
						Regex.Replace(categoryPath, "ItemList_(\\d+)_" + oldFields.muid + "_(\\d+).csv$", "ItemList_$1_" + fields.muid + "_$2.csv")
					);
				}
			}
		}
		catch (Exception e)
		{
			throw e;
		}
	}

	private void UpdateCategories(string path, Func<string, string> LambdaReplace)
	{
		List<CsvUtils.StudioCategory> categories = CsvUtils.DeserializeCsvStudio<CsvUtils.StudioCategory>(path);
		if (categories.Count != 0)
		{
			foreach (CsvUtils.StudioCategory category in categories)
			{
				category.categoryNumber = LambdaReplace(category.categoryNumber);
			}
			CsvUtils.WriteToCsv(path, categories);
		}
	}

	private void UpdateItems(string path, Func<string, string> LambdaReplace)
	{
		List<CsvUtils.StudioItem> items = CsvUtils.DeserializeCsvStudio<CsvUtils.StudioItem>(path);
		if (items.Count != 0)
		{
			foreach (CsvUtils.StudioItem item in items)
			{
				item.groupNumber = itemGroupName == "3DSE" ? "11" : fields.muid;
				item.categoryNumber = LambdaReplace(item.categoryNumber);
			}
			CsvUtils.WriteToCsv(path, items);
		}
	}
}