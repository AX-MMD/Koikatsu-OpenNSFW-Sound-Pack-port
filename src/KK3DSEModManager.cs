using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Studio.Sound;
using System.Text;
using System.Linq;
using ActionGame.MapSound;
using IllusionMods.KoikatsuStudioCsv;

namespace IllusionMods.Koikatsu3DSEModTools {

	public class KK3DSEModManager
	{
		public string modPath;
		public string modName;
		public string prefabOutputPath;
		public string sourcesPath;
		public string basePrefabPath;
		public bool useLegacyClassifier;
		public int maxPerCategory;
		public UnityEngine.GameObject basePrefab;

		public KK3DSEModManager(string selectedPath, bool useLegacyClassifier = false, int maxPerCategory = int.MaxValue)
		{
			this.useLegacyClassifier = useLegacyClassifier;
			this.maxPerCategory = maxPerCategory;
			this.modPath = Utils.GetModPath(selectedPath, true);
			this.modName = Utils.GetModName(this.modPath);
			this.prefabOutputPath = Path.Combine(this.modPath, "Prefab");
			if (!Directory.Exists(prefabOutputPath))
			{
				Directory.CreateDirectory(prefabOutputPath);
				Debug.Log("Created output directory: " + prefabOutputPath);
			}
			this.sourcesPath = Path.Combine(this.modPath, "Sources");
			if (!Directory.Exists(sourcesPath))
			{
				throw new Exception("Sources folder not found at path: " + sourcesPath);
			}
			this.basePrefabPath = Path.Combine(this.modPath, "base_3dse.prefab");
			if (!File.Exists(this.basePrefabPath))
			{
				throw new Exception("Base prefab not found at path: " + this.basePrefabPath);
			}
			this.basePrefab = (GameObject)AssetDatabase.LoadAssetAtPath(this.basePrefabPath, typeof(GameObject));
			if (this.basePrefab == null)
			{
				throw new Exception("Base prefab not found at path: " + this.basePrefabPath);
			}
		}

		public int GenerateCSV(bool create, bool isSidePanel, List<Category> categories)
		{
			Utils.Tuple<string> paths = CsvUtils.GetCsvItemFilePaths(this.modPath);
			string categoryPath = paths.Item1;
			string itemListPath = paths.Item2;

			foreach (string path in paths)
			{
				if (!File.Exists(path))
				{
					throw new Exception("CSV file not found at path: " + path);
				}
			}

			Dictionary<string, CsvStudioCategory> newCategories = new Dictionary<string, CsvStudioCategory>();
			Dictionary<string, CsvStudioCategory> oldCategories = new Dictionary<string, CsvStudioCategory>();
			if (!(create && isSidePanel))
			{
				foreach (CsvStudioCategory category in CsvUtils.DeserializeCsvStudio<CsvStudioCategory>(categoryPath))
				{
					oldCategories[category.GetKey()] = category;
				}
			}

			Dictionary<string, CsvStudioItem> newEntries = new Dictionary<string, CsvStudioItem>();
			Dictionary<string, CsvStudioItem> oldEntries = new Dictionary<string, CsvStudioItem>();
			if (!(create && isSidePanel))
			{
				foreach (CsvStudioItem entry in CsvUtils.DeserializeCsvStudio<CsvStudioItem>(itemListPath))
				{
					oldEntries[entry.GetKey()] = entry;
				}
			}

			int id = 1;
			if (!(create && isSidePanel) && oldEntries.Count > 0)
			{
				id = oldEntries.Values.Min(x => int.Parse(x.GetID())) + 1;
			}

			Utils.Tuple<string> info = CsvUtils.GetCsvModInfo(itemListPath);
			string groupNumber = info.Item1;
			string categoryNumber = info.Item2;

			if (!(create && isSidePanel) && oldCategories.Count > 0)
			{
				categoryNumber = Utils.GetLastValue(oldCategories.Values).GetID();
			}

			categories.Sort((x, y) => string.Compare(x.author, y.author));

			float categoryCount = 0.0f;
			int totalCount = 0;
			string progressName = create ? "Generating " + this.modName : "Updating " + this.modName;

			foreach (Category category in categories)
			{
				string categoryKey = category.GetKey();
				string currentCategoryNumber = categoryNumber;
				string bundlePath = this.GetAssetBundlePath(categoryKey);

				categoryCount++;
				EditorUtility.DisplayProgressBar(
					progressName, 
					string.Format("Category ({0}/{1}): {2}", categoryCount, categories.Count, categoryKey),
					(float)categoryCount / categories.Count
				);

				if (oldCategories.ContainsKey(categoryKey))
				{
					currentCategoryNumber = oldCategories[categoryKey].GetID();
					newCategories[categoryKey] = oldCategories[categoryKey];
				}
				else
				{
					newCategories[categoryKey] = new CsvStudioCategory(currentCategoryNumber, categoryKey);
					// Increment to the next category number, takes effect on next iteration
					categoryNumber = (int.Parse(categoryNumber) + 1).ToString(new string('0', categoryNumber.Length));
				}

				foreach (StudioItemParam item in category.items)
				{
					totalCount++;
					string itemKey = item.prefabName + currentCategoryNumber;
					if (newEntries.ContainsKey(itemKey))
					{
						throw new Exception("Duplicate entry '"+ item.prefabName +"' for category '"+ categoryKey +"' with item name '"+ item.itemName +"'.");
					}
					else if (oldEntries.ContainsKey(itemKey))
					{
						// Update existing entry
						CsvStudioItem entry = oldEntries[itemKey];
						entry.groupNumber = groupNumber;
						entry.categoryNumber = currentCategoryNumber;
						entry.name = item.itemName;
						entry.bundlePath = bundlePath;
						entry.fileName = item.prefabName;
						newEntries[itemKey] = entry;
					}
					else
					{
						// Add new entry
						newEntries[itemKey] = new CsvStudioItem(id.ToString(), groupNumber, currentCategoryNumber, item.itemName, "", bundlePath, item.prefabName, "", false, false, false, false, false, false, false, false, false);
						id++;
					}
				}
			}

			CsvUtils.WriteToCsv(categoryPath, newCategories.Values.ToList());
			CsvUtils.WriteToCsv(itemListPath, newEntries.Values.ToList());
			Debug.Log("CSV files generated successfully.");
			return totalCount;
		}

		public int GeneratePrefabs(bool create, bool isSidePanel, List<Category> categories)
		{
			if (!Directory.Exists(this.prefabOutputPath))
			{
				Directory.CreateDirectory(this.prefabOutputPath);
				Debug.Log("Created output directory: " + this.prefabOutputPath);
			}
			else if (create && isSidePanel)
			{
				// Clear the output directory
				DirectoryInfo directoryInfo = new DirectoryInfo(this.prefabOutputPath);
				foreach (FileInfo file in directoryInfo.GetFiles())
				{
					file.Delete();
				}
				foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
				{
					dir.Delete(true);
				}
				Debug.Log("Cleared output directory: " + this.prefabOutputPath);
			}

			int prefabTotalCount = 0;
			int categoryCount = 0;
			string progressName = create ? "Generating " + this.modName : "Updating " + this.modName;

			HashSet<string> oldPrefabs = new HashSet<string>(Directory.GetFiles(this.prefabOutputPath, "*.prefab", SearchOption.AllDirectories));

			foreach (Category category in categories)
			{
				int itemCount = 0;
				string categoryOutputPath = Path.Combine(this.prefabOutputPath, category.GetKey()).Replace("\\", "/");
				string assetBundlePath = this.GetAssetBundlePath(category.GetKey());

				categoryCount++;
				EditorUtility.DisplayProgressBar(
					progressName, 
					string.Format("Category ({0}/{1}): {2} -> ({3}/{4})", categoryCount, categories.Count, category.GetKey(), itemCount, category.items.Count),
					(float)itemCount / category.items.Count
				);

				if (!Directory.Exists(categoryOutputPath))
				{
					Directory.CreateDirectory(categoryOutputPath);
					Debug.Log("Created output directory: " + categoryOutputPath);
				}
				else if (create && isSidePanel)
				{
					// Clear the output directory
					DirectoryInfo directoryInfo = new DirectoryInfo(categoryOutputPath);
					foreach (FileInfo file in directoryInfo.GetFiles())
					{
						file.Delete();
					}
					foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
					{
						dir.Delete(true);
					}
					Debug.Log("Cleared output directory: " + categoryOutputPath);
				}

				foreach (StudioItemParam file in category.items)
				{
					string newPrefabPath = Path.Combine(categoryOutputPath, file.prefabName + ".prefab").Replace("\\", "/");
					GameObject newObject;

					if (oldPrefabs.Contains(newPrefabPath))
					{
						oldPrefabs.Remove(newPrefabPath);
						//update the existing prefab
						newObject = (GameObject)AssetDatabase.LoadAssetAtPath(newPrefabPath, typeof(GameObject));
					}
					else
					{
						//create a new prefab, if there is already a prefab with the same name, it will be overwritten
						newObject = (GameObject)PrefabUtility.InstantiatePrefab(this.basePrefab);
					}

					// Load the AudioClip from the .wav file
					AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(file.path);
					if (audioClip == null)
					{
						throw new Exception("AudioClip not found at path: " + file.path);
					}

					// Assign the AudioClip to the SEComponent
					SEComponent seComponent = newObject.GetComponent<SEComponent>();
					if (seComponent != null)
					{
						seComponent._clip = audioClip;
						if (file.prefabModifier.isLoop)
						{
							seComponent._isLoop = true;
						}
						if (file.prefabModifier.threshold != null)
						{
							seComponent._rolloffDistance = new Threshold(file.prefabModifier.threshold.Item1, file.prefabModifier.threshold.Item2);
						}
						if (file.prefabModifier.volume != -1.0f)
						{
							seComponent._volume = file.prefabModifier.volume;
						}
					}
					else
					{
						throw new Exception("SEComponent not found on the instantiated prefab.");
					}

					PrefabUtility.CreatePrefab(newPrefabPath, newObject);
					UnityEngine.Object.DestroyImmediate(newObject);

					// Set the asset bundle name in the .meta file
					string metaFilePath = newPrefabPath + ".meta";
					if (File.Exists(metaFilePath))
					{
						Utils.SetAssetBundleNameInMetaFile(metaFilePath, assetBundlePath);
					}
					else
					{
						throw new Exception("Meta file not found for prefab: " + newPrefabPath);
					}

					itemCount++;
					EditorUtility.DisplayProgressBar(
						progressName, 
						string.Format("Category ({0}/{1}): {2} -> ({3}/{4})", categoryCount, categories.Count, category.GetKey(), itemCount, category.items.Count),
						(float)itemCount / category.items.Count
					);
				}
				prefabTotalCount += itemCount;
				itemCount = 0;
			}
			// Delete any old prefabs that were not updated
			foreach (string oldPrefab in oldPrefabs)
			{
				File.Delete(oldPrefab);
			}
			return prefabTotalCount;
		}

		public List<Category> GetCategories()
		{
			List<Category> categories = new List<Category>();
			string[] rootFolders = Directory.GetDirectories(this.sourcesPath);
			if (rootFolders.Length == 0)
			{
				categories.Add(new Category("Default", "", this.GetCategoryFiles(this.sourcesPath, new string[] { "keep-name" })));
			}
			else
			{
				foreach (string rootFolder in rootFolders)
				{
					string rootFolderName = Path.GetFileName(rootFolder);
					List<string> tags = TagManager.GetTags(rootFolder, new string[] { "indexed" }, this.useLegacyClassifier);
					if (rootFolderName.StartsWith("[") && rootFolderName.EndsWith("]") && Directory.GetDirectories(rootFolder).Length > 0)
					{
						categories.Add(new Category(rootFolderName));
						foreach (string subfolder in Directory.GetDirectories(rootFolder))
						{
							categories.AddRange(this.GetCategoriesRecursive(subfolder, tags));
						}
					}
					else
					{
						categories.AddRange(this.GetCategoriesRecursive(rootFolder, tags));
					}
				}
			}
			return categories;
		}

		private List<Category> GetCategoriesRecursive(string folder, ICollection<string> cumulTags = null)
		{
			string folderName = Path.GetFileName(folder);
			Match match = Regex.Match(folderName, @"^(?<categoryName>[^()]+)(\((?<author>[^)]+)\))?$");
			string categoryName = this.useLegacyClassifier ? match.Groups["categoryName"].Value.Trim() : folderName;
			string author = match.Groups["author"].Value.Trim();

			List<string> tags = TagManager.GetTags(folder, cumulTags, this.useLegacyClassifier);
			List<Category> categories = new List<Category>();

			if (!this.useLegacyClassifier || !string.IsNullOrEmpty(author))
			{
				if (tags.Contains("skip-folder-name"))
				{
					tags.RemoveAll(item => item == "skip-folder-name");
					categories.Add(new Category(categoryName, author, this.GetCategoryFiles(folder, tags)));
				}
				else
				{
					categories.Add(new Category(categoryName, author, this.GetCategoryFiles(folder, tags, Utils.ToItemCase(categoryName))));
				}
				return categories;
			}
			else
			{
				tags.RemoveAll(item => item == "skip-folder-name");
				foreach (string subfolder in Directory.GetDirectories(folder))
				{
					categories.AddRange(this.GetCategoriesRecursive(subfolder, tags));
				}
			}

			return categories;
		}

		private List<StudioItemParam> GetCategoryFiles(string folder, ICollection<string> cumulTags, string pathName = "")
		{
			List<StudioItemParam> items = new List<StudioItemParam>();
			List<string> entries = new List<string>(Directory.GetFileSystemEntries(folder));
			entries.Sort(new NaturalSortComparer());

			int index = 1;
			foreach (string entry in entries)
			{
				if (Directory.Exists(entry))
				{
					string folderName = Path.GetFileName(entry);
					List<string> tags = TagManager.GetTags(entry, cumulTags, this.useLegacyClassifier);

					if (this.useLegacyClassifier)
					{
						if (folderName.ToUpper().Contains("FX") || folderName.ToUpper().Contains("ORIGINAL"))
						{
							continue;
						}
						else if (folderName.ToUpper() == "NORMAL")
						{
							tags.Add("skip-folder-name");
						}
					}

					if (tags.Contains("skip-folder-name"))
					{
						tags.Remove("skip-folder-name");
						items.AddRange(this.GetCategoryFiles(entry, tags, pathName));
					}
					else if (folderName.ToUpper() == folderName)
					{
						items.AddRange(this.GetCategoryFiles(entry, tags, pathName + folderName));
					}
					else
					{
						items.AddRange(this.GetCategoryFiles(entry, tags, pathName + Utils.ToItemCase(folderName)));
					}
				}
				else if (AudioProcessor.IsValidAudioFile(entry) && index <= this.maxPerCategory)
				{
					items.Add(new StudioItemParam(
						this.BuildItemName(pathName, cumulTags, entry, index++), 
						entry, 
						TagManager.GetPrefabModifier(cumulTags)
					));
				}
			}
			return items;
		}

		private string BuildItemName(string pathName, ICollection<string> tags, string filename, int index)
		{
			if (this.useLegacyClassifier && (Path.GetFileNameWithoutExtension(filename).ToLower().Contains("loop") || Path.GetFileNameWithoutExtension(filename).ToLower().Contains("full")) && !tags.Contains("loop"))
			{
				throw new Exception("Looping sound file detected: " + filename + ". Please add the 'loop' tag to the folder.");
			}

			string name = string.IsNullOrEmpty(pathName) ? Utils.ToItemCase(Path.GetFileNameWithoutExtension(filename)) : pathName;
			return TagManager.ApplyNameModifierTags(name, tags, filename, index);
		}

		private string GetAssetBundlePath(string categoryKey)
		{
			return "studio/" + Utils.ToZipmodFileName(Utils.GetModGuid(this.modPath)) + "/" + Utils.ToZipmodFileName(categoryKey) + "/bundle.unity3d";
		}
	}

}