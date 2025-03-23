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
using IllusionMods.Koikatsu3DSECategoryTool;

public class New3DSEMod : MonoBehaviour
{
	[MenuItem("Assets/3DSE/New 3DSE Mod", false, 0)]
	public static void MakeNew3DSEMod(MenuCommand command)
	{
		Modify3DSEMod(true, command);
	}

	[MenuItem("Assets/3DSE/Edit 3DSE Mod", true)]
	public static bool ValidateEdit3DSEMod()
	{
		return Utils.GetSelected3DSEModPaths().Count == 1;
	}

	[MenuItem("Assets/3DSE/Edit 3DSE Mod", false, 1)]
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
			sourcePath = "Assets/Examples/Studio 3DSE Template";
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
	private static string itemGroupName;
	private static string oldItemGroupName;
	private static CsvUtils.ItemFileAggregate itemFileAgg;
	private static Utils.ManifestInfo fields;
	private static Utils.ManifestInfo oldFields;
	private static string[] studioTabs;
	private static int studioTabMode;
	private static string firstCategory;

	public Modify3DSEModWindow()
	{
	}

	public static void ShowWindow(string srcPath, string destPath, bool create)
	{
		sourcePath = srcPath;
		destinationPath = destPath;
		createMode = create;
		itemGroupName = "3DSE";
		oldItemGroupName = null;
		fields = new Utils.ManifestInfo(version: "1.0");
		oldFields = null;
		studioTabs = new string[] { "3DSE", "Custom" };
		studioTabMode = 0;
		firstCategory = "01";
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

	private enum GroupChangeState
	{
		From3DSETo3DSE,
		From3DSEToCustom,
		FromCustomTo3DSE,
		FromCustomToCustom,
		NoChange
	}

	private static GroupChangeState GetGroupChangeState(string oldName, string newName)
	{
		if (oldName == newName)
		{
			return GroupChangeState.NoChange;
		}
		else if (oldName == "3DSE")
		{
			if (newName == "3DSE")
			{
				return GroupChangeState.From3DSETo3DSE;
			}
			else
			{
				return GroupChangeState.From3DSEToCustom;
			}
		}
		else
		{
			if (newName == "3DSE")
			{
				return GroupChangeState.FromCustomTo3DSE;
			}
			else
			{
				return GroupChangeState.FromCustomToCustom;
			}
		}
	}

	private static string ConvertCategoryNumber(string oldCategoryNumber, GroupChangeState changeState)
	{
		if (string.IsNullOrEmpty(oldCategoryNumber))
		{
			return oldCategoryNumber;
		}
		else if (changeState == GroupChangeState.From3DSEToCustom)
		{
			return Regex.Replace(oldCategoryNumber, "^" + fields.muid + "(\\d+)$", "$1");
		}
		else if (changeState == GroupChangeState.FromCustomTo3DSE)
		{
			return Regex.Replace(oldCategoryNumber, "^(\\d+)$", fields.muid + "$1");
		}
		else
		{
			return oldCategoryNumber;
		}
	}

	private static void LoadForEdit(string path)
	{
		oldFields = Utils.ManifestInfo.Load(Path.Combine(path, "manifest.xml"));
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

		if (itemGroupName == "3DSE")
		{
			studioTabMode = 0;
			firstCategory = ConvertCategoryNumber(itemFileAgg.modCategoryNumber, GroupChangeState.From3DSEToCustom);
		}
		else
		{
			studioTabMode = 1;
			firstCategory = itemFileAgg.modCategoryNumber;
		}
	}

	private static void EnsureDataFilesFolderIntegrity(CsvUtils.ItemFileAggregate itemFileAgg)
	{
		if ( ! (new string[] { itemFileAgg.GetDefaultGroupFile(), itemFileAgg.GetDefaultCategoryFile(), itemFileAgg.GetDefaultListFile() }.Contains(null)))
		{
			return; // All files are present, all is good
		}

		if ( ! EditorUtility.DisplayDialog("Rebuild Data Files", "Some List/Studio files are missing or corrupt, try to rebuild?", "Yes", "No"))
		{
			throw new Exception("Rebuild aborted by user");
		}

		// If null, both ItemGroup and ItemCategory files are missing or have an incorrect name.
		string groupNumber = itemFileAgg.modGroupNumber;
		// If null, ItemList file is missing or has an incorrect name.
		string categoryNumber = itemFileAgg.modCategoryNumber; 
		// If both ItemCategory and ItemList files are missing, rebuild assuming default (11, 3DSE).
		string groupName;

		// ItemGroup integrity
		if (itemFileAgg.GetDefaultGroupFile() == null || itemFileAgg.IsEmptyEntries<CsvUtils.StudioGroup>())
		{
			groupNumber = groupNumber ?? fields.muid;
			groupName = (groupNumber == "11" || groupNumber == null) ? "3DSE" : fields.name;
			CsvUtils.WriteToCsv(
				Path.Combine(itemFileAgg.csvFolder, "ItemGroup_" + Path.GetFileName(itemFileAgg.csvFolder) + ".csv"),
				new CsvUtils.StudioGroup[] { new CsvUtils.StudioGroup(groupNumber, groupName) }
			);
		}
		else
		{
			groupNumber = itemFileAgg.GetFirstEntry<CsvUtils.StudioGroup>().groupNumber;
		}

		// ItemCategory integrity
		if (itemFileAgg.GetDefaultCategoryFile() == null)
		{
			File.Create(Path.Combine(itemFileAgg.csvFolder, "ItemCategory_00_" + groupNumber + ".csv")).Close();
			itemFileAgg.Refresh();
		}


		// ItemList integrity
		if (itemFileAgg.GetDefaultListFile() == null)
		{
			File.Create(Path.Combine(itemFileAgg.csvFolder, "ItemList_00_" + groupNumber + "_" + categoryNumber + ".csv")).Close();
		}
		else if (categoryNumber == null)
		{
			// Is incorrect ItemList file name, renaming file.
			CsvUtils.StudioCategory first = itemFileAgg.GetFirstEntry<CsvUtils.StudioCategory>();
			if (first != null)
			{
				categoryNumber = first.categoryNumber;
			}
			else if (!itemFileAgg.IsEmptyEntries<CsvUtils.StudioItem>())
			{
				categoryNumber = itemFileAgg.GetFirstEntry<CsvUtils.StudioItem>().categoryNumber;
			}
			else
			{
				if (groupNumber != "11")
				{
					categoryNumber = "01";
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

			Utils.FileMove(itemFileAgg.GetDefaultListFile(), Path.Combine(itemFileAgg.csvFolder, "ItemList_00_" + groupNumber + "_" + categoryNumber + ".csv"));
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
		studioTabMode = EditorGUILayout.Popup("Tab", studioTabMode, studioTabs);

		if (studioTabMode == 0)
		{
			itemGroupName = "3DSE";
		}
		else
		{
			if (!createMode && oldItemGroupName != "3DSE")
			{
				itemGroupName = oldItemGroupName;
			}
			else if (itemGroupName == "3DSE")
			{
				itemGroupName = "";
			}
			itemGroupName = EditorGUILayout.TextField("Name", itemGroupName);
		}

		firstCategory = EditorGUILayout.TextField("First Category #", firstCategory);

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

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(createMode ? "Create" : "Save", GUILayout.Width(position.width / 2)) && (createMode || IsChanged()))
		{
			try
			{
				ValidateFields();
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
				itemFileAgg.Refresh();
			}
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.Width(position.width / 2)))
		{
			this.Close();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	private bool IsChanged()
	{
		return Utils.ManifestInfo.Load(Path.Combine(sourcePath, "manifest.xml")) != fields 
			|| itemGroupName != oldItemGroupName
			|| firstCategory != itemFileAgg.modCategoryNumber;
	}

	private bool ValidateFields()
	{
		List<string> errors = fields.Validate();

		int _;
		if (!int.TryParse(firstCategory, out _))
		{
			errors.Add("First Category # must be a number: " + firstCategory);
		}

		if (itemGroupName == "")
		{
			errors.Add("Studio Item Tab is required.");
		}
		else if (itemGroupName == "3DSE" && (fields.muid.Length + firstCategory.Length) > 8)
		{
			errors.Add("For the 3DSE tab, Mod UID + First Category must be 8 digits or less.");
		}

		if (errors.Count > 0)
		{
			throw new Exception(string.Join("\n", errors.ToArray()));
		}

		return true;
	}

	private void CreateMod()
	{
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
					Path.Combine(listPath, "ItemList_00_" + fields.muid + "_" + firstCategory + ".csv")
				);
			}

			CsvUtils.WriteToCsv(Path.Combine(listPath, "ItemGroup_DataFiles.csv"), group);
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("Success", "Mod created successfully.", "OK");
			this.Close();
			ShowWindow(newDestinationPath, newDestinationPath, false);
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
			bool majorEdit = false;
			GroupChangeState changeState = GetGroupChangeState(oldItemGroupName, itemGroupName);
			if ((changeState != GroupChangeState.NoChange || firstCategory != itemFileAgg.modCategoryNumber) && itemFileAgg.GetFirstEntry<CsvUtils.StudioCategory>() != null)
			{
				if ( ! EditorUtility.DisplayDialog("Major Edit", "Changing 3DSE <-> Custom or First Category # will re-create CSV files from Sources. \n\nContinue?", "Yes", "No"))
				{
					throw new Exception("Rebuild aborted by user");
				}
				else
				{
					majorEdit = true;
				}
			}

			if (changeState != GroupChangeState.NoChange)
			{
				List<CsvUtils.StudioGroup> groups = itemFileAgg.GetEntries<CsvUtils.StudioGroup>();
				if (groups.Count == 0)
				{
					groups.Add(new CsvUtils.StudioGroup(itemGroupName == "3DSE" ? "11" : fields.muid, itemGroupName));
				}
				else
				{
					groups[0].name = itemGroupName;
					groups[0].groupNumber = itemGroupName == "3DSE" ? "11" : fields.muid;
				}
				CsvUtils.WriteToCsv(itemFileAgg.GetDefaultGroupFile(), groups);
			}

			string categoryPath = itemFileAgg.GetDefaultCategoryFile();
			string listPath = itemFileAgg.GetDefaultListFile();

			if (changeState == GroupChangeState.From3DSEToCustom)
			{
				Utils.FileMove(
					categoryPath,
					Regex.Replace(categoryPath, "ItemCategory_(\\d+)_11.csv$", "ItemCategory_$1_" + fields.muid + ".csv")
				);
				Utils.FileMove(
					listPath,
					Regex.Replace(listPath, "ItemList_(\\d+)_11_" + oldFields.muid + "(\\d+).csv$", "ItemList_$1_" + fields.muid + "_" + firstCategory + ".csv")
				);
			}
			else if (changeState == GroupChangeState.FromCustomTo3DSE)
			{
				Utils.FileMove(
					categoryPath,
					Regex.Replace(categoryPath, "ItemCategory_(\\d+)_" + oldFields.muid + ".csv$", "ItemCategory_$1_11.csv")
				);
				Utils.FileMove(
					listPath,
					Regex.Replace(listPath, "ItemList_(\\d+)_" + oldFields.muid + "_(\\d+).csv$", "ItemList_$1_11_" + fields.muid + firstCategory + ".csv")
				);
			}
			else if (changeState != GroupChangeState.NoChange)
			{
				Utils.FileMove(
					categoryPath,
					Regex.Replace(categoryPath, "ItemCategory_(\\d+)_" + oldFields.muid + ".csv$", "ItemCategory_$1_" + fields.muid + ".csv")
				);
				Utils.FileMove(
					listPath,
					Regex.Replace(listPath, "ItemList_(\\d+)_" + oldFields.muid + "_(\\d+).csv$", "ItemList_$1_" + fields.muid + "_" + firstCategory + ".csv")
				);
			} 
			else if (firstCategory != itemFileAgg.modCategoryNumber)
			{
				Utils.FileMove(
					listPath,
					Regex.Replace(listPath, "ItemList_(\\d+)_" + fields.muid + "_(\\d+).csv$", "ItemList_$1_" + fields.muid + "_" + firstCategory + ".csv")
				);
			}

			if (majorEdit)
			{
				// Rebuild CSV files from Sources
				List<Category> categories = new CategoryManager().BuildFromSource(KK3DSEModManager.GetSourceFolderPath(sourcePath));
				Utils.GenerationResult countA = CsvUtils.GenerateCSV(sourcePath, true, categories);
				EditorUtility.ClearProgressBar();
				Debug.Log("CSV -> Created: " + countA.createCount + " Updated: " + countA.updateCount + " Deleted: " + countA.deleteCount);
			}

			fields.Save(Path.Combine(sourcePath, "manifest.xml"));
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("Success", "Mod edited successfully.", "OK");
			LoadForEdit(sourcePath);
		}
		catch (Exception e)
		{
			throw e;
		}
	}
}