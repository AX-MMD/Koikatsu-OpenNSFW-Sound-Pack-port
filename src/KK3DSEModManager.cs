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

		public List<Category> getCategories()
		{
			List<Category> categories = new List<Category>();
			string[] rootFolders = Directory.GetDirectories(this.sourcesPath);
			if (rootFolders.Length == 0)
			{
				categories.Add(new Category("Default", "", this.getCategoryFiles(this.sourcesPath, new List<string>(new string[] { "keep-name" }))));
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
							categories.AddRange(this.recursiveGetCategories(subfolder, new List<string>()));
						}
					}
					else
					{
						categories.AddRange(this.recursiveGetCategories(rootFolder, new List<string>()));
					}
				}
			}
			return categories;
		}

		public List<string> getCategoriesFromItemList()
		{
			Utils.Tuple<string> paths = Utils.GetCsvItemFilePaths(this.modPath);
			string itemListPath = paths.Item2;

			if (!File.Exists(itemListPath))
			{
				throw new Exception("CSV file not found at path: " + itemListPath);
			}

			List<string> categories = new List<string>();
			string[] lines = File.ReadAllLines(itemListPath);
			foreach (string line in lines)
			{
				string[] columns = line.Split(',');
				if (columns.Length > 2)
				{
					categories.Add(columns[2].Trim());
				}
			}
			return categories;
		}

		public void generateCSV(bool create, bool isSidePanel, List<Category> categories)
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
			List<string> categoryLines;
			if (create && isSidePanel)
			{
				categoryLines = Utils.GetEmptyItemCategoryCsv();
			}
			else
			{
				categoryLines = new List<string>();
				string[] lines = File.ReadAllLines(categoryPath);
				foreach (string line in lines)
				{
					categoryLines.Add(line);
					string[] columns = line.Split(',');
					if (columns.Length > 1)
					{
						existingCategories[columns[1].Trim()] = columns;
					}
				}
			}

			Dictionary<string, string[]> existingEntries = new Dictionary<string, string[]>();
			List<string> itemListLines;
			if (create && isSidePanel)
			{
				itemListLines = Utils.GetEmptyItemListCsv();
			}
			else
			{
				itemListLines = new List<string> ();
				string[] lines = File.ReadAllLines(itemListPath);
				foreach (string line in lines)
				{
					string[] columns = line.Split(',');
					if (columns.Length > 6)
					{
						existingEntries[columns[6].Trim()] = columns;
					}
				}
			}

			int id = 1;
			if (!(create && isSidePanel) && existingEntries.Count > 0)
			{
				id = int.Parse(this.getLastValue(existingEntries.Values)[0]) + 1;
			}

			Utils.Tuple<string> info = Utils.GetCsvModInfo(itemListPath);
			string groupNumber = info.Item1;
			string categoryNumber = info.Item2;

			if (!(create && isSidePanel) && existingCategories.Count > 0)
			{
				categoryNumber = this.getLastValue(existingCategories.Values)[0];
			}

			categories.Sort((x, y) => string.Compare(x.author, y.author));

			foreach (Category category in categories)
			{
				string categoryKey = category.getKey();
				string currentCategoryNumber = categoryNumber;
				if (existingCategories.ContainsKey(categoryKey))
				{
					currentCategoryNumber = existingCategories[categoryKey][0];
				}
				else
				{
					existingCategories[categoryKey] = new string[] { currentCategoryNumber, categoryKey };
					categoryLines.Add(string.Join(",", existingCategories[categoryKey]));
					categoryNumber = (int.Parse(categoryNumber) + 1).ToString(new string('0', categoryNumber.Length));
				}

				foreach (SoundFile file in category.files)
				{
					string bundlePath = this.getAssetBundlePath();
					string itemName = file.itemName;
					if (create && isSidePanel || !existingEntries.ContainsKey(file.prefabName))
					{
						// Add new entry
						string[] newEntry = new string[] { id.ToString(), groupNumber, currentCategoryNumber, itemName, "", bundlePath, file.prefabName, "", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE" };
						if (existingEntries.ContainsKey(file.prefabName) && existingEntries[file.prefabName][2] == currentCategoryNumber)
						{
							throw new Exception("Duplicate entry '"+ file.prefabName +"' for category '"+ categoryKey +"' with item name '"+ itemName +"'.");
						}
						existingEntries[file.prefabName] = newEntry;
						itemListLines.Add(string.Join(",", newEntry));
						id++;
					}
					else
					{
						// Update existing entry
						string[] existingEntry = existingEntries[file.prefabName];
						existingEntry[1] = groupNumber;
						existingEntry[2] = currentCategoryNumber;
						existingEntry[3] = itemName;
						existingEntry[5] = bundlePath;
						existingEntry[6] = file.prefabName;
						itemListLines.Add(string.Join(",", existingEntry));
					}
				}
			}

			Utils.WriteToCsv(categoryPath, categoryLines);
			Utils.WriteToCsv(itemListPath, itemListLines);
			Debug.Log("CSV files generated successfully.");
		}

		public void generatePrefabs(bool create, bool isSidePanel, List<Category> categories)
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
				string categoryOutputPath = Path.Combine(this.prefabOutputPath, category.getKey()).Replace("\\", "/");
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
						if (file.isLoop)
						{
							seComponent._isLoop = true;
						}
						if (file.threshold != null)
						{
							seComponent._rolloffDistance = new Threshold(file.threshold.Item1, file.threshold.Item2);
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
						Utils.SetAssetBundleNameInMetaFile(metaFilePath, this.getAssetBundlePath());
					}
					else
					{
						throw new Exception("Meta file not found for prefab: " + newPrefabPath);
					}

					prefabCount++;
				}
				Debug.Log(string.Format("{0} Prefabs generated successfully for Category '{1}'", prefabCount, category.getKey()));
				prefabTotalCount += prefabCount;
				prefabCount = 0;
			}
			AssetDatabase.Refresh();
			Debug.Log(string.Format("Generate {0} Prefabs from Sources for {1} Categories.", prefabTotalCount, categories.Count));
		}

		private T getLastValue<T>(IEnumerable<T> collection)
		{
			T last = default(T);
			foreach (T item in collection)
			{
				last = item;
			}
			return last;
		}

		private string toItemCase(string input)
		{
			if (string.IsNullOrEmpty(input)) return input;

			string[] words = Regex.Split(Utils.ToValidKoiFileName(input).Replace("-", "_"), @"[\s_]+");
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

		private List<Category> recursiveGetCategories(string folder, List<string> cumulTags)
		{
			string folderName = Path.GetFileName(folder);
			Match match = Regex.Match(folderName, @"^(?<categoryName>[^()]+)(\((?<author>[^)]+)\))?$");
			string categoryName = this.useLegacyClassifier ? match.Groups["categoryName"].Value.Trim() : folderName;
			string author = match.Groups["author"].Value.Trim();

			List<string> tags = TagManager.CombineTags(cumulTags, TagManager.GetTags(folder), this.useLegacyClassifier);
			List<Category> categories = new List<Category>();

			if (!this.useLegacyClassifier || !string.IsNullOrEmpty(author))
			{
				if (tags.Contains("skip-folder-name"))
				{
					tags.Remove("skip-folder-name");
					categories.Add(new Category(categoryName, author, this.getCategoryFiles(folder, tags)));
				}
				else
				{
					categories.Add(new Category(categoryName, author, this.getCategoryFiles(folder, tags, this.toItemCase(categoryName))));
				}
				return categories;
			}
			else
			{
				tags.Remove("skip-folder-name");
				foreach (string subfolder in Directory.GetDirectories(folder))
				{
					categories.AddRange(this.recursiveGetCategories(subfolder, tags));
				}
			}

			return categories;
		}

		private List<SoundFile> getCategoryFiles(string folder, List<string> cumulTags, string pathName = "")
		{
			List<SoundFile> files = new List<SoundFile>();
			int index = 1;

			foreach (string entry in Directory.GetFileSystemEntries(folder))
			{
				if (Directory.Exists(entry))
				{
					string folderName = Path.GetFileName(entry);
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
						files.AddRange(this.getCategoryFiles(entry, tags, pathName));
					}
					else if (folderName.ToUpper() == folderName)
					{
						files.AddRange(this.getCategoryFiles(entry, tags, pathName + folderName));
					}
					else
					{
						files.AddRange(this.getCategoryFiles(entry, tags, pathName + this.toItemCase(folderName)));
					}
				}
				else if (AudioProcessor.IsValidAudioFile(entry) && index <= this.maxPerCategory)
				{
					Utils.Tuple<float> threshold = null;
					foreach (string tag in cumulTags)
					{
						Match thresholdMatch = Regex.Match(tag, @"threshold-(?<minValue>\d+(\.\d+)?)-(?<maxValue>\d+(\.\d+)?)");
						if (thresholdMatch.Success)
						{
							threshold = new Utils.Tuple<float>(float.Parse(thresholdMatch.Groups["minValue"].Value), float.Parse(thresholdMatch.Groups["maxValue"].Value));
							break;
						}
					}
					files.Add(new SoundFile(this.buildItemName(pathName, cumulTags, entry, index++), entry, cumulTags.Contains("loop"), threshold));
				}
			}
			return files;
		}

		private string buildItemName(string pathName, List<string> tags, string filename, int index)
		{
			if (this.useLegacyClassifier && (Path.GetFileNameWithoutExtension(filename).ToLower().Contains("loop") || Path.GetFileNameWithoutExtension(filename).ToLower().Contains("full")) && !tags.Contains("loop"))
			{
				throw new Exception("Looping sound file detected: " + filename + ". Please add the 'loop' tag to the folder.");
			}

			if (tags.Contains("keep-name"))
			{
				return Path.GetFileNameWithoutExtension(filename);
			}

			string name = string.IsNullOrEmpty(pathName) ? this.toItemCase(Path.GetFileNameWithoutExtension(filename)) : pathName;
			for (int i = tags.Count - 1; i >= 0; i--)
			{
				string tag = tags[i];
				Match appendMatch = Regex.Match(tag, @"append-(?<appendValue>.+)");
				if (appendMatch.Success)
				{
					name += appendMatch.Groups["appendValue"].Value;
				}

				Match prependMatch = Regex.Match(tag, @"prepend-(?<prependValue>.+)");
				if (prependMatch.Success)
				{
					name = prependMatch.Groups["prependValue"].Value + name;
				}
			}

			if (tags.Contains("appendfilename"))
			{
				name += Path.GetFileNameWithoutExtension(filename);
			}
			if (tags.Contains("prependfilename"))
			{
				name = Path.GetFileNameWithoutExtension(filename) + name;
			}

			if (tags.Contains("no-index"))
			{
				return name;
			}
			else
			{
				return name + (index > 9 ? index.ToString() : "0" + index.ToString());
			}
		}

		private string getAssetBundlePath()
		{
			return Path.Combine(Path.Combine("studio", Utils.ToValidKoiFileName(Utils.GetModGuid(this.modPath))), "bundle.unity3d");
		}
	}

}