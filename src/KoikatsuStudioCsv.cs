using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IllusionMods.Koikatsu3DSEModTools;
using IllusionMods.Koikatsu3DSECategoryTool;

namespace IllusionMods.KoikatsuStudioCsv
{
	public static class CsvUtils
	{
		public abstract class BaseCsvStudio
		{
			public abstract string GetID();
			public abstract string GetKey();
			public abstract override string ToString();
		}

		public class StudioGroup : BaseCsvStudio
		{
			private string _groupNumber;
			public string groupNumber 
			{ 
				set 
				{
					int _;
					if (!int.TryParse(value, out _))
					{
						throw new FormatException("Group Number was not a number: " + value);
					}
					_groupNumber = value;
				}
				get { return _groupNumber; }
			}
			public string name { get; set; }

			public static List<string> GetHeaders()
			{
				return new List<string> { "Group Number,Name" };
			}

			public StudioGroup(string groupNumber, string name)
			{
				this.groupNumber = groupNumber;
				this.name = name;
			}

			public StudioGroup(string csvLine)
			{
				var values = csvLine.Split(',');
				groupNumber = values[0];
				name = values[1];
			}

			public override string GetID()
			{
				return groupNumber;
			}

			public override string GetKey()
			{
				return name;
			}

			public override string ToString()
			{
				var values = new string[] { groupNumber, name };
				return string.Join(",", values);
			}
		}

		public class StudioCategory : BaseCsvStudio
		{
			private string _categoryNumber;
			public string categoryNumber 
			{ 
				set 
				{
					int _;
					if (!int.TryParse(value, out _))
					{
						throw new FormatException("Category Number was not a number: " + value);
					}
					_categoryNumber = value;
				}
				get { return _categoryNumber; }
			}
			public string name { get; set; }

			public static List<string> GetHeaders()
			{
				return new List<string> { "Category Number,Name" };
			}

			public StudioCategory(string categoryNumber, string name)
			{
				this.categoryNumber = categoryNumber;
				this.name = name;
			}

			public StudioCategory(string csvLine)
			{
				var values = csvLine.Split(',');
				categoryNumber = values[0];
				name = values[1];
			}

			public override string GetID()
			{
				return categoryNumber;
			}

			public override string GetKey()
			{
				return name;
			}

			public override string ToString()
			{
				var values = new string[] { categoryNumber, name };
				return string.Join(",", values);
			}
		}

		public class StudioItem : BaseCsvStudio
		{
			private string _id;
			private string _groupNumber;
			private string _categoryNumber;
			public string id 
			{
				set
				{
					int _;
					if (!int.TryParse(value, out _))
					{
						throw new FormatException("ID was not a number: " + value);
					}
					_id = value;
				}
				get { return _id; }
			}
			public string groupNumber 
			{
				set
				{
					int _;
					if (!int.TryParse(value, out _))
					{
						throw new FormatException("Group Number was not a number: " + value);
					}
					_groupNumber = value;
				}
				get { return _groupNumber; }
			}
			public string categoryNumber 
			{ 
				set 
				{
					int _;
					if (!int.TryParse(value, out _))
					{
						throw new FormatException("Category Number was not a number: " + value);
					}
					_categoryNumber = value;
				}
				get { return _categoryNumber; }
			}
			public string name { get; set; }
			public string manifest { get; set; }
			public string bundlePath { get; set; }
			public string fileName { get; set; }
			public string childAttachmentTransform { get; set; }
			public bool animation { get; set; }
			public bool color1 { get; set; }
			public bool pattern1 { get; set; }
			public bool color2 { get; set; }
			public bool pattern2 { get; set; }
			public bool color3 { get; set; }
			public bool pattern3 { get; set; }
			public bool scaling { get; set; }
			public bool emission { get; set; }

			public static List<string> GetHeaders()
			{
				return new List<string> { "ID,Group Number,Category Number,Name,Manifest,Bundle Path,File Name,Child Attachment Transform,Animation,Color1,Pattern1,Color2,Pattern2,Color3,Pattern3,Scaling,Emission" };
			}

			public StudioItem(string id, string groupNumber, string categoryNumber, string name, string manifest, string bundlePath, string fileName, string childAttachmentTransform, bool animation, bool color1, bool pattern1, bool color2, bool pattern2, bool color3, bool pattern3, bool scaling, bool emission)
			{
				this.id = id;
				this.groupNumber = groupNumber;
				this.categoryNumber = categoryNumber;
				this.name = name;
				this.manifest = manifest;
				this.bundlePath = bundlePath;
				this.fileName = fileName;
				this.childAttachmentTransform = childAttachmentTransform;
				this.animation = animation;
				this.color1 = color1;
				this.pattern1 = pattern1;
				this.color2 = color2;
				this.pattern2 = pattern2;
				this.color3 = color3;
				this.pattern3 = pattern3;
				this.scaling = scaling;
				this.emission = emission;
			}

			public StudioItem(string csvLine)
			{
				var values = csvLine.Split(',');
				id = values[0];
				groupNumber = values[1];
				categoryNumber = values[2];
				name = values[3];
				manifest = values[4];
				bundlePath = values[5];
				fileName = values[6];
				childAttachmentTransform = values[7];
				animation = values[8] == "TRUE";
				color1 = values[9] == "TRUE";
				pattern1 = values[10] == "TRUE";
				color2 = values[11] == "TRUE";
				pattern2 = values[12] == "TRUE";
				color3 = values[13] == "TRUE";
				pattern3 = values[14] == "TRUE";
				scaling = values[15] == "TRUE";
				emission = values[16] == "TRUE";
			}

			public override string GetID()
			{
				return id;
			}

			public override string GetKey()
			{
				return fileName + categoryNumber;
			}

			public override string ToString()
			{
				var values = new string[]
				{
					id, groupNumber, categoryNumber, name, manifest, bundlePath, fileName, childAttachmentTransform,
					animation ? "TRUE" : "FALSE",
					color1 ? "TRUE" : "FALSE",
					pattern1 ? "TRUE" : "FALSE",
					color2 ? "TRUE" : "FALSE",
					pattern2 ? "TRUE" : "FALSE",
					color3 ? "TRUE" : "FALSE",
					pattern3 ? "TRUE" : "FALSE",
					scaling ? "TRUE" : "FALSE",
					emission ? "TRUE" : "FALSE"
				};
				return string.Join(",", values);
			}
		}

		public struct ItemFileAggregate
		{
			private string[] _groupFiles;
			private string[] _categoryFiles; 
			private string[] _listFiles;
			public string[] groupFiles 
			{ 
				get { return (string[])_groupFiles.Clone (); }
			}
			public string[] categoryFiles 
			{
				get { return (string[])_categoryFiles.Clone (); }
			}
			public string[] listFiles 
			{
				get { return (string[])_listFiles.Clone (); }
			}
			public string csvFolder { get; private set; }

			public ItemFileAggregate(string csvFolder, string[] groupFiles, string[] categoryFiles, string[] listFiles)
			{
				this.csvFolder = csvFolder;
				this._groupFiles = groupFiles.OrderBy(x => x, new NaturalSortComparer()).ToArray();
				this._categoryFiles = categoryFiles.OrderBy(x => x, new NaturalSortComparer()).ToArray();
				this._listFiles = listFiles.OrderBy(x => x, new NaturalSortComparer()).ToArray();
			}

			public string GetDefaultGroupFile()
			{
				return _groupFiles.Length > 0 ? _groupFiles[0] : null;
			}

			public string GetDefaultCategoryFile()
			{
				return _categoryFiles.Length > 0 ? _categoryFiles[0] : null;
			}

			public string GetDefaultListFile()
			{
				return _listFiles.Length > 0 ? _listFiles[0] : null;
			}

			public T GetFirstEntry<T>() where T : BaseCsvStudio
			{
				string path = null;
				if (typeof(T) == typeof(StudioGroup))
				{
					path = GetDefaultGroupFile();
				}
				else if (typeof(T) == typeof(StudioCategory))
				{
					path = GetDefaultCategoryFile();
				}
				else if (typeof(T) == typeof(StudioItem))
				{
					path = GetDefaultListFile();
				}
				else
				{
					throw new InvalidOperationException("Invalid type: " + typeof(T).ToString());
				}

				foreach (T entry in DeserializeCsvStudio<T>(path))
				{
					return entry;
				}

				return null;
			}

			public Utils.Tuple<string> GetModInfo(bool raise_exec = false)
			{
				Match match = Regex.Match(Path.GetFileName(GetDefaultListFile()), @"ItemList_(\d+)_(\d+)_(\d+).csv$");
				if (!match.Success)
				{
					if (raise_exec)
					{
						throw new FormatException("Incorrect ItemList_*csv name, format should be ItemList_<number>_<number>_<number>.csv: " + GetDefaultListFile());
					}
					else
					{
						match = Regex.Match(Path.GetFileName(GetDefaultCategoryFile()), @"ItemCategory_(\d+)_(\d+).csv$");
						return Utils.Tuple<string>.Create (match.Success ? match.Groups [2].Value : null, null);
					}
				}
				else
				{
					return Utils.Tuple<string>.Create(match.Groups[2].Value, match.Groups[3].Value);
				}
			}

			public string GetStudioItemTabName()
			{
				if (!File.Exists(GetDefaultGroupFile()))
				{
					return null;
				}

				foreach (StudioGroup group in CsvUtils.DeserializeCsvStudio<StudioGroup>(GetDefaultGroupFile()))
				{
					return group.name;
				}

				return null;
			}

			public void Refresh()
			{
				_groupFiles = Directory.GetFiles(csvFolder, "ItemGroup_*.csv").OrderBy(x => x, new NaturalSortComparer()).ToArray();
				_categoryFiles = Directory.GetFiles(csvFolder, "ItemCategory_*.csv").OrderBy(x => x, new NaturalSortComparer()).ToArray();
				_listFiles = Directory.GetFiles(csvFolder, "ItemList_*.csv").OrderBy(x => x, new NaturalSortComparer()).ToArray();
			}
		} 

		private class MultiCsvNotSupported : Exception
		{
			public MultiCsvNotSupported(string message) : base(message) { }
		}

		public static string GetItemDataFolder(string path)
		{
			string listPath = Path.Combine(Utils.GetModPath(path, true), "List/Studio");	
			try
			{
				return GetItemDataFolderRecursive(listPath);
			}
			catch (MultiCsvNotSupported)
			{
				throw new Exception("Multiple folders with CSV files is not supported: " + listPath);
			}

		}

		private static string GetItemDataFolderRecursive(string path)
		{
			string[] folders = Directory.GetDirectories(path);
			string csvFolder = null;
			foreach (string folder in folders)
			{
				string tmp = null;
				string[] files = Directory.GetFiles(folder, "*.csv");
				if (files.Length > 0)
				{
					tmp = folder;
				}
				else
				{
					string subFolder = GetItemDataFolderRecursive(folder);
					if (subFolder != null)
					{
						tmp = subFolder;
					}
				}

				if (tmp != null && csvFolder != null)
				{
					throw new MultiCsvNotSupported("Multiple folders with CSV files are not supported.");
				}

				csvFolder = tmp;
			}
			return csvFolder;
		}

		public static ItemFileAggregate GetItemFileAggregate(string path)
		{
			string csvFolder = GetItemDataFolder(Utils.GetModPath(path, true));
			if (csvFolder == null)
			{
				throw new Exception("No folder with CSV files found in List/Studio: " + path);
			}
			return new ItemFileAggregate(
				csvFolder,
				Directory.GetFiles(csvFolder, "ItemGroup_*.csv"),
				Directory.GetFiles(csvFolder, "ItemCategory_*.csv"),
				Directory.GetFiles(csvFolder, "ItemList_*.csv")
			);
		}

		public static void WriteToCsv<T>(string filePath, IEnumerable<T> items) where T : BaseCsvStudio
		{
			SerializeCsvStudio(filePath, items);
		}

		public static List<T> DeserializeCsvStudio<T>(string filePath) where T : BaseCsvStudio
		{
			List<T> items = new List<T>();
			string[] lines = File.ReadAllLines(filePath);
			for (int i = 1; i < lines.Length; i++)
			{
				items.Add((T)Activator.CreateInstance(typeof(T), lines[i]));
			}
			return items.OrderBy(x => int.Parse(x.GetID())).ToList();
		}

		public static string[] DeserializeCsvStudioIDs(string filePath)
		{
			string[] lines = File.ReadAllLines(filePath);
			string[] ids = new string[lines.Length - 1];
			for (int i = 1; i < lines.Length; i++)
			{
				ids[i - 1] = lines[i].Split(',')[0];
			}
			return ids;
		}

		public static void SerializeCsvStudio<T>(string filePath, IEnumerable<T> items) where T : BaseCsvStudio
		{
			List<string> lines = GetHeaders<T>();
			foreach (T item in items.OrderBy(x => int.Parse(x.GetID())))
			{
				lines.Add(item.ToString());
			}

			File.WriteAllText(filePath, string.Join("\n", lines.ToArray()), System.Text.Encoding.UTF8);
		}

		public static List<string> GetHeaders<T>() where T : BaseCsvStudio
		{
			if (typeof(T) == typeof(StudioGroup))
			{
				return StudioGroup.GetHeaders();
			}
			else if (typeof(T) == typeof(StudioCategory))
			{
				return StudioCategory.GetHeaders();
			}
			else if (typeof(T) == typeof(StudioItem))
			{
				return StudioItem.GetHeaders();
			}
			else
			{
				throw new Exception("Invalid type");
			}
		}

		public static Utils.GenerationResult GenerateCSV(string modPath, bool create, IList<Category> categories)
		{
			CsvUtils.ItemFileAggregate csvAgg = CsvUtils.GetItemFileAggregate(modPath);
			string categoryPath = csvAgg.GetDefaultCategoryFile();
			string itemListPath = csvAgg.GetDefaultListFile();

			foreach (string path in new string[] { categoryPath, itemListPath })
			{
				if (!File.Exists(path))
				{
					throw new Exception("CSV file not found at path: " + path);
				}
			}

			Utils.Tuple<string> info = csvAgg.GetModInfo(true);
			string groupNumber = info.Item1;
			string categoryNumber = info.Item2;
			int id = 1;

			Dictionary<string, StudioCategory> newCategories = new Dictionary<string, StudioCategory>();
			Dictionary<string, StudioCategory> oldCategories = new Dictionary<string, StudioCategory>();
			Dictionary<string, StudioItem> newEntries = new Dictionary<string, StudioItem>();
			Dictionary<string, StudioItem> oldEntries = new Dictionary<string, StudioItem>();

			if (!create)
			{
				foreach (StudioCategory category in CsvUtils.DeserializeCsvStudio<StudioCategory>(categoryPath))
				{
					oldCategories[category.GetKey()] = category;
				}
				foreach (StudioItem entry in CsvUtils.DeserializeCsvStudio<StudioItem>(itemListPath))
				{
					oldEntries[entry.GetKey()] = entry;
				}

				if (oldEntries.Count > 0)
				{
					id = oldEntries.Values.Max(x => int.Parse(x.GetID())) + 1;
				}
				if (oldCategories.Count > 0)
				{
					categoryNumber = Utils.GetLastValue(oldCategories.Values).GetID();
				}
			}

			float categoryCount = 0.0f;
			int createCount = 0;
			int updateCount = 0;
			string progressName = create ? "Generating " + Utils.GetModName(modPath) : "Updating " + Utils.GetModName(modPath);

			foreach (Category category in categories.OrderBy(x => x.author).ThenBy(x => x.name))
			{
				string categoryKey = category.GetKey();
				string currentCategoryNumber = categoryNumber;
				string bundlePath = Utils.GetAssetBundlePath(modPath, categoryKey);

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
					newCategories[categoryKey] = new StudioCategory(currentCategoryNumber, categoryKey);
					// Increment to the next category number, takes effect on next iteration
					categoryNumber = (int.Parse(categoryNumber) + 1).ToString(new string('0', categoryNumber.Length));
				}

				foreach (StudioItemParam item in category.items)
				{
					string itemKey = item.prefabName + currentCategoryNumber;
					if (newEntries.ContainsKey(itemKey))
					{
						throw new Exception("Duplicate entry '"+ item.prefabName +"' for category '"+ categoryKey +"' with item name '"+ item.itemName +"'.");
					}
					else if (oldEntries.ContainsKey(itemKey))
					{
						// Update existing entry
						StudioItem entry = oldEntries[itemKey];
						entry.groupNumber = groupNumber;
						entry.categoryNumber = currentCategoryNumber;
						entry.name = item.itemName;
						entry.bundlePath = bundlePath;
						entry.fileName = item.prefabName;
						newEntries[itemKey] = entry;
						updateCount++;
					}
					else
					{
						// Add new entry
						newEntries[itemKey] = new StudioItem(id.ToString(), groupNumber, currentCategoryNumber, item.itemName, "", bundlePath, item.prefabName, "", false, false, false, false, false, false, false, false, false);
						id++;
						createCount++;
					}
				}
			}

			CsvUtils.WriteToCsv(categoryPath, newCategories.Values.ToList());
			CsvUtils.WriteToCsv(itemListPath, newEntries.Values.ToList());
			return new Utils.GenerationResult
			{
				createCount = createCount,
				updateCount = updateCount,
				deleteCount = oldEntries.Count - updateCount
			};
		}
	}
}