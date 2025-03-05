using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IllusionMods.Koikatsu3DSEModTools;

namespace IllusionMods.KoikatsuStudioCsv
{

	public static class CsvUtils
	{
		public static string GetItemDataFolder(string modPath)
		{
			string path = Path.Combine(modPath, "List/Studio/DataFiles");
			if (!Directory.Exists(path))
			{
				throw new Exception("Missing List/Studio/DataFiles folder in mod directory: " + modPath);
			}

			return path;
		}

		public static Utils.Tuple<string> GetCsvItemFilePaths(string modPath)
		{
			string csvFolder = GetItemDataFolder(modPath);
			string[] listFiles = Directory.GetFiles(csvFolder, "ItemList_00*.csv");
			string[] categoryFiles = Directory.GetFiles(csvFolder, "ItemCategory_00*.csv");
			return Utils.Tuple<string>.Create(
				categoryFiles.Length > 0 ? categoryFiles[0] : null, 
				listFiles.Length > 0 ? listFiles[0] : null
			);
		}

		public static void WriteToCsv<T>(string filePath, List<T> items) where T : BaseCsvStudio
		{
			SerializeCsvStudio(filePath, items);
		}

		public static Utils.Tuple<string> GetCsvModInfo(string itemListPath)
		{
			Match match = Regex.Match(Path.GetFileName(itemListPath), @"ItemList_(\d+)_(\d+)_(\d+)");
			if (!match.Success)
			{
				return Utils.Tuple<string>.Create(null, null);
			}
			else
			{
				return Utils.Tuple<string>.Create(match.Groups[2].Value, match.Groups[3].Value);
			}
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
			if (typeof(T) == typeof(CsvStudioGroup))
			{
				return CsvStudioGroup.GetHeaders();
			}
			else if (typeof(T) == typeof(CsvStudioCategory))
			{
				return CsvStudioCategory.GetHeaders();
			}
			else if (typeof(T) == typeof(CsvStudioItem))
			{
				return CsvStudioItem.GetHeaders();
			}
			else
			{
				throw new Exception("Invalid type");
			}
		}
	}

	public abstract class BaseCsvStudio
	{
		public abstract string GetID();
		public abstract string GetKey();
		public abstract override string ToString();
	}

	public class CsvStudioGroup : BaseCsvStudio
	{
		public string groupNumber { get; set; }
		public string name { get; set; }

		public static List<string> GetHeaders()
		{
			return new List<string> { "Group Number,Name" };
		}

		public CsvStudioGroup(string groupNumber, string name)
		{
			this.groupNumber = groupNumber;
			this.name = name;
		}

		public CsvStudioGroup(string csvLine)
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

	public class CsvStudioCategory : BaseCsvStudio
	{
		public string categoryNumber { get; set; }
		public string name { get; set; }

		public static List<string> GetHeaders()
		{
			return new List<string> { "Category Number,Name" };
		}

		public CsvStudioCategory(string categoryNumber, string name)
		{
			this.categoryNumber = categoryNumber;
			this.name = name;
		}

		public CsvStudioCategory(string csvLine)
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

	public class CsvStudioItem : BaseCsvStudio
	{
		public string id { get; set; }
		public string groupNumber { get; set; }
		public string categoryNumber { get; set; }
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

		public CsvStudioItem(string id, string groupNumber, string categoryNumber, string name, string manifest, string bundlePath, string fileName, string childAttachmentTransform, bool animation, bool color1, bool pattern1, bool color2, bool pattern2, bool color3, bool pattern3, bool scaling, bool emission)
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

		public CsvStudioItem(string csvLine)
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
}