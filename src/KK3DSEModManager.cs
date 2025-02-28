using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Studio.Sound;
using System.Text;
using ActionGame.MapSound;

namespace IllusionMods.Koikatsu3DSEModTools {

	public class KK3DSEModManager
	{
		public string modPath;
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
			this.modPath = Utils.GetModPath(selectedPath);
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

		public void GenerateCSV(bool create, bool isSidePanel, List<Category> categories)
		{
			Utils.Tuple<string> paths = Utils.GetCsvItemFilePaths(this.modPath);
			string categoryPath = paths.Item1;
			string itemListPath = paths.Item2;

			foreach (string path in paths)
			{
				if (!File.Exists(path))
				{
					throw new Exception("CSV file not found at path: " + path);
				}
			}

			Dictionary<string, string[]> existingCategories = new Dictionary<string, string[]>();
			if (!(create && isSidePanel))
			{
				List<string> lines = new List<string>(File.ReadAllLines(categoryPath));
				if (lines.Count > 0)
				{
					lines.RemoveAt(0);
				}
				foreach (string line in lines)
				{
					string[] columns = line.Split(',');
					existingCategories[columns[1].Trim()] = columns;
				}
			}

			Dictionary<string, string[]> existingEntries = new Dictionary<string, string[]>();
			if (!(create && isSidePanel))
			{
				List<string> lines = new List<string>(File.ReadAllLines(itemListPath));
				if (lines.Count > 0)
				{
					lines.RemoveAt(0);
				}
				foreach (string line in lines)
				{
					string[] columns = line.Split(',');
					// The key is the prefab name + category number
					existingEntries[columns[6].Trim() + columns[2].Trim()] = columns;
				}
			}

			int id = 1;
			if (!(create && isSidePanel) && existingEntries.Count > 0)
			{
				id = int.Parse(Utils.GetLastValue(existingEntries.Values)[0]) + 1;
			}

			Utils.Tuple<string> info = Utils.GetCsvModInfo(itemListPath);
			string groupNumber = info.Item1;
			string categoryNumber = info.Item2;

			if (!(create && isSidePanel) && existingCategories.Count > 0)
			{
				categoryNumber = Utils.GetLastValue(existingCategories.Values)[0];
			}

			categories.Sort((x, y) => string.Compare(x.author, y.author));

			foreach (Category category in categories)
			{
				string categoryKey = category.GetKey();
				string currentCategoryNumber = categoryNumber;
				string bundlePath = this.GetAssetBundlePath(categoryKey);
				if (existingCategories.ContainsKey(categoryKey))
				{
					currentCategoryNumber = existingCategories[categoryKey][0];
				}
				else
				{
					existingCategories[categoryKey] = new string[] { currentCategoryNumber, categoryKey };
					categoryNumber = (int.Parse(categoryNumber) + 1).ToString(new string('0', categoryNumber.Length));
				}

				foreach (SoundFile file in category.files)
				{
					string itemKey = file.prefabName + currentCategoryNumber;
					if (existingEntries.ContainsKey(itemKey))
					{
						if (create && isSidePanel)
						{
							throw new Exception("Duplicate entry '"+ file.prefabName +"' for category '"+ categoryKey +"' with item name '"+ file.itemName +"'.");
						}
						// Update existing entry
						string[] existingEntry = existingEntries[itemKey];
						existingEntry[1] = groupNumber;
						existingEntry[2] = currentCategoryNumber;
						existingEntry[3] = file.itemName;
						existingEntry[5] = bundlePath;
						existingEntry[6] = file.prefabName;
					}
					else
					{
						// Add new entry
						string[] newEntry = new string[] { id.ToString(), groupNumber, currentCategoryNumber, file.itemName, "", bundlePath, file.prefabName, "", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE" };
						existingEntries[itemKey] = newEntry;
						id++;
					}
				}
			}
			List<string> categoryLines = new List<string>(Utils.GetItemCategoryHeaders());
			foreach (string[] columns in existingCategories.Values)
			{
				categoryLines.Add(string.Join(",", columns));
			}
			List<string> itemListLines = new List<string>(Utils.GetItemListHeaders());
			foreach (string[] columns in existingEntries.Values)
			{
				itemListLines.Add(string.Join(",", columns));
			}


			Utils.WriteToCsv(categoryPath, categoryLines);
			Utils.WriteToCsv(itemListPath, itemListLines);
			Debug.Log("CSV files generated successfully.");
		}

		public void GeneratePrefabs(bool create, bool isSidePanel, List<Category> categories)
		{
			Debug.Log("Generate Prefabs From Source started...");
			Debug.Log("Source path: " + this.sourcesPath);
			Debug.Log("Output path: " + this.prefabOutputPath);

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
			foreach (Category category in categories)
			{
				int prefabCount = 0;
				string categoryOutputPath = Path.Combine(this.prefabOutputPath, category.GetKey()).Replace("\\", "/");
				string assetBundlePath = this.GetAssetBundlePath(category.GetKey());

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

				foreach (SoundFile file in category.files)
				{
					string newPrefabPath = Path.Combine(categoryOutputPath, file.prefabName + ".prefab").Replace("\\", "/");
					GameObject newObject;

					if (!create && File.Exists(newPrefabPath))
					{
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

					prefabCount++;
				}
				Debug.Log(string.Format("{0} Prefabs generated successfully for Category '{1}'", prefabCount, category.GetKey()));
				prefabTotalCount += prefabCount;
				prefabCount = 0;
			}
			AssetDatabase.Refresh();
			Debug.Log(string.Format("Generate {0} Prefabs from Sources for {1} Categories.", prefabTotalCount, categories.Count));
		}

		public List<Category> GetCategories()
		{
			List<Category> categories = new List<Category>();
			string[] rootFolders = Directory.GetDirectories(this.sourcesPath);
			if (rootFolders.Length == 0)
			{
				categories.Add(new Category("Default", "", this.GetCategoryFiles(this.sourcesPath, new List<string>(new string[] { "keep-name" }))));
			}
			else
			{
				foreach (string rootFolder in rootFolders)
				{
					string rootFolderName = Path.GetFileName(rootFolder);
					if (rootFolderName.StartsWith("[") && rootFolderName.EndsWith("]") && Directory.GetDirectories(rootFolder).Length > 0)
					{
						categories.Add(new Category(rootFolderName));
						foreach (string subfolder in Directory.GetDirectories(rootFolder))
						{
							categories.AddRange(this.GetCategoriesRecursive(subfolder, new List<string>()));
						}
					}
					else
					{
						categories.AddRange(this.GetCategoriesRecursive(rootFolder, new List<string>()));
					}
				}
			}
			return categories;
		}

		private List<Category> GetCategoriesRecursive(string folder, List<string> cumulTags)
		{
			string folderName = Path.GetFileName(folder);
			Match match = Regex.Match(folderName, @"^(?<categoryName>[^()]+)(\((?<author>[^)]+)\))?$");
			string categoryName = this.useLegacyClassifier ? match.Groups["categoryName"].Value.Trim() : folderName;
			string author = match.Groups["author"].Value.Trim();
			
			TagManager.ConvertTagsToNewVersion(folder); //TODO: remove this
			List<string> tags = TagManager.CombineTags(cumulTags, TagManager.GetTags(folder), this.useLegacyClassifier);
			List<Category> categories = new List<Category>();

			if (!this.useLegacyClassifier || !string.IsNullOrEmpty(author))
			{
				if (tags.Contains("skip-folder-name"))
				{
					tags.Remove("skip-folder-name");
					categories.Add(new Category(categoryName, author, this.GetCategoryFiles(folder, tags)));
				}
				else
				{
					categories.Add(new Category(categoryName, author, this.GetCategoryFiles(folder, tags, this.ToItemCase(categoryName))));
				}
				return categories;
			}
			else
			{
				tags.Remove("skip-folder-name");
				foreach (string subfolder in Directory.GetDirectories(folder))
				{
					categories.AddRange(this.GetCategoriesRecursive(subfolder, tags));
				}
			}

			return categories;
		}

		private List<SoundFile> GetCategoryFiles(string folder, List<string> cumulTags, string pathName = "")
		{
			List<SoundFile> files = new List<SoundFile>();
			int index = 1;
			
			List<string> entries = new List<string>(Directory.GetFileSystemEntries(folder));
			entries.Sort(new NaturalSortComparer());

			foreach (string entry in entries)
			{
				if (Directory.Exists(entry))
				{
					string folderName = Path.GetFileName(entry);
					TagManager.ConvertTagsToNewVersion(folder); //TODO: remove this
					List<string> tags = TagManager.CombineTags(cumulTags, TagManager.GetTags(entry), this.useLegacyClassifier);

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
						files.AddRange(this.GetCategoryFiles(entry, tags, pathName));
					}
					else if (folderName.ToUpper() == folderName)
					{
						files.AddRange(this.GetCategoryFiles(entry, tags, pathName + folderName));
					}
					else
					{
						files.AddRange(this.GetCategoryFiles(entry, tags, pathName + this.ToItemCase(folderName)));
					}
				}
				else if (AudioProcessor.IsValidAudioFile(entry) && index <= this.maxPerCategory)
				{
					files.Add(new SoundFile(
						this.BuildItemName(pathName, cumulTags, entry, index++), 
						entry, 
						TagManager.GetPrefabModifier(cumulTags)
					));
				}
			}
			return files;
		}

		private string BuildItemName(string pathName, List<string> tags, string filename, int index)
		{
			if (this.useLegacyClassifier && (Path.GetFileNameWithoutExtension(filename).ToLower().Contains("loop") || Path.GetFileNameWithoutExtension(filename).ToLower().Contains("full")) && !tags.Contains("loop"))
			{
				throw new Exception("Looping sound file detected: " + filename + ". Please add the 'loop' tag to the folder.");
			}

			string name = string.IsNullOrEmpty(pathName) ? this.ToItemCase(Path.GetFileNameWithoutExtension(filename)) : pathName;
			return TagManager.ApplyNameModifierTags(name, tags, filename, index);
		}

		private string ToItemCase(string input)
		{
			if (string.IsNullOrEmpty(input)) return input;

			string[] words = Regex.Split(Utils.ToZipmodFileName(input).Replace("-", "_"), @"[\s_]+");
			StringBuilder itemCase = new StringBuilder();

			foreach (string word in words)
			{
				if (word.Length > 0)
				{
					if (char.ToUpper(word[0]) == word[0])
					{
						itemCase.Append(word);
					}
					else
					{
						itemCase.Append(char.ToUpper(word[0]));
						if (word.Length > 1)
						{
							itemCase.Append(word.Substring(1).ToLower());
						}
					}
				}
			}

			return itemCase.ToString();
		}

		private string GetAssetBundlePath(string categoryKey)
		{
			return "studio/" + Utils.ToZipmodFileName(Utils.GetModGuid(this.modPath)) + "/" + Utils.ToZipmodFileName(categoryKey) + "/bundle.unity3d";
		}
	}

}