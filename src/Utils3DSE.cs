using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Studio.Sound;
using IllusionMods.KoikatuModdingTools;

namespace IllusionMods.Koikatsu3DSEModTools {

public static class Utils
{

	public class Tuple<T>
	{
		private T _itme1;
		private T _item2;
		public T Item1
		{
			get { return _itme1; }
		}
		public T Item2
		{
			get { return _item2; }
		}

		public Tuple(T item1, T item2)
		{
			_itme1 = item1;
			_item2 = item2;
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield return Item1;
			yield return Item2;
		}

		public T this[int index]
		{
			get
			{
				switch (index)
				{
					case 0:
						return Item1;
					case 1:
						return Item2;
					default:
						throw new IndexOutOfRangeException();
				}
			}
		}

		public static Tuple<T> Create(T item1, T item2)
		{
			return new Tuple<T>(item1, item2);
		}
	}

	public class ManifestInfo
	{
		public string filePath;
		public string guid;
		public string author;
		public string name;
		public string muid;
		public string version;
		public string description;
		public string website;

		public ManifestInfo(string manifestPath)
		{
			XmlDocument xmlDoc = new XmlDocument();
			if (!File.Exists(manifestPath)) {
				XmlElement manifest = xmlDoc.CreateElement("manifest");
				xmlDoc.AppendChild(manifest);
			} else {
				xmlDoc.Load(manifestPath);
			}

			this.filePath = manifestPath;

			XmlNode guidNode = xmlDoc.SelectSingleNode("//guid");
			if (guidNode == null) {
				xmlDoc.CreateElement("guid");
				this.guid = "";
			} else {
				this.guid = guidNode.InnerText;
			}

			XmlNode authorNode = xmlDoc.SelectSingleNode("//author");
			if (authorNode == null) {
				xmlDoc.CreateElement("author");
				this.author = "";
			} else {
				this.author = authorNode.InnerText;
			}

			XmlNode nameNode = xmlDoc.SelectSingleNode("//name");
			if (nameNode == null) {
				xmlDoc.CreateElement("name");
				this.name = "";
			} else {
				this.name = nameNode.InnerText;
			}

			XmlNode muidNode = xmlDoc.SelectSingleNode("//muid");
			if (muidNode == null) {
				xmlDoc.CreateElement("muid");
				this.muid = "";
			} else {
				this.muid = muidNode.InnerText;
			}

			XmlNode versionNode = xmlDoc.SelectSingleNode("//version");
			if (versionNode == null) {
				xmlDoc.CreateElement("version");
				this.version = "";
			} else {
				this.version = versionNode.InnerText;
			}

			XmlNode descriptionNode = xmlDoc.SelectSingleNode("//description");
			if (descriptionNode == null) {
				xmlDoc.CreateElement("description");
				this.description = "";
			} else {
				this.description = descriptionNode.InnerText;
			}

			XmlNode websiteNode = xmlDoc.SelectSingleNode("//website");
			if (websiteNode == null) {
				xmlDoc.CreateElement("website");
				this.website = "";
			} else {
				this.website = websiteNode.InnerText;
			}
		}

		public ManifestInfo(string guid = "", string author = "", string name = "", string muid = "", string version = "", string description = "", string website = "")
		{
			this.guid = guid;
			this.author = author;
			this.name = name;
			this.muid = muid;
			this.version = version;
			this.description = description;
			this.website = website;
		}

		public ManifestInfo(Dictionary<string, string> fields)
		{
			this.guid = fields.ContainsKey("guid") ? fields["guid"] : "";
			this.author = fields.ContainsKey("author") ? fields["author"] : "";
			this.name = fields.ContainsKey("name") ? fields["name"] : "";
			this.muid = fields.ContainsKey("muid") ? fields["muid"] : "";
			this.version = fields.ContainsKey("version") ? fields["version"] : "";
			this.description = fields.ContainsKey("description") ? fields["description"] : "";
			this.website = fields.ContainsKey("website") ? fields["website"] : "";
		}

		public void edit(string guid = null, string author = null, string name = null, string muid = null, string version = null, string description = null, string website = null)
		{
			if (guid != null) {
				this.guid = guid;
			}
			if (author != null) {
				this.author = author;
			}
			if (name != null) {
				this.name = name;
			}
			if (muid != null) {
				this.muid = muid;
			}
			if (version != null) {
				this.version = version;
			}
			if (description != null) {
				this.description = description;
			}
			if (website != null) {
				this.website = website;
			}
		}

		public void edit(Dictionary<string, string> fields)
		{
			this.edit(
				fields.ContainsKey("guid") ? fields["guid"] : null,
				fields.ContainsKey("author") ? fields["author"] : null,
				fields.ContainsKey("name") ? fields["name"] : null,
				fields.ContainsKey("muid") ? fields["muid"] : null,
				fields.ContainsKey("version") ? fields["version"] : null,
				fields.ContainsKey("description") ? fields["description"] : null,
				fields.ContainsKey("website") ? fields["website"] : null
			);
		}

		public Dictionary<string, string> toDictionary()
		{
			return new Dictionary<string, string>
			{
				{ "guid", this.guid },
				{ "author", this.author },
				{ "name", this.name },
				{ "muid", this.muid },
				{ "version", this.version },
				{ "description", this.description },
				{ "website", this.website },
			};
		}

		public List<string> validate()
		{
			List<string> errors = new List<string>();
			if (string.IsNullOrEmpty(this.guid))
			{
				errors.Add("GUID is required.");
			}
			if (string.IsNullOrEmpty(this.author))
			{
				errors.Add("Author name is required.");
			}
			if (string.IsNullOrEmpty(this.name))
			{
				errors.Add("Mod name is required.");
			}
			if (string.IsNullOrEmpty(this.muid))
			{
				errors.Add("MUID is required.");
			}
			else if (!Regex.IsMatch(this.muid, @"^\d{3,6}$"))
			{
				errors.Add("MUID must be a 3-6 digit number.");
			}
			return errors;
		}

		public void save()
		{
			this.save(this.filePath);
		}

		public void save(string manifestPath)
		{
			if (string.IsNullOrEmpty(manifestPath))
			{
				throw new Exception("Manifest filePath is required.");
			}

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(manifestPath);

			foreach (KeyValuePair<string, string> field in this.toDictionary())
			{
				if (field.Value == null)
				{
					continue;
				}

				XmlNode node = xmlDoc.SelectSingleNode("//" + field.Key);
				if (node != null)
				{
					node.InnerText = field.Value;
				}
				else
				{
					XmlNode rootNode = xmlDoc.SelectSingleNode("//manifest");
					XmlElement element = xmlDoc.CreateElement(field.Key);
					element.InnerText = field.Value;
					rootNode.AppendChild(element);
				}
			}

			List<string> errors = this.validate ();
			if (errors.Count == 0)
			{
				xmlDoc.Save(manifestPath);
			}
			else
			{
				throw new Exception(string.Join("\n", errors.ToArray()));
			}
		}

	}

	public static void GetSelectedFolderPath(out string selectedPath, out bool isSidePanel)
	{
		if (Selection.assetGUIDs.Length > 0)
		{
			selectedPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[Selection.assetGUIDs.Length - 1]);
			isSidePanel = true;
		}
		else if (Selection.activeObject != null)
		{
			selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			isSidePanel = false;
		}
		else
		{
			throw new Exception("Please select a folder in the Assets/Mods directory.");
		}

		if (string.IsNullOrEmpty(selectedPath))
		{
			throw new Exception("Selection cannot be empty");
		}

		if (isSidePanel)
		{
			string parentFolder = Path.GetFileName(Path.GetDirectoryName(selectedPath));
			if (!Directory.Exists(selectedPath) || parentFolder != "Mods")
			{
				Debug.LogError("Selection parent folder is '" + parentFolder + "' while it should be 'Mods'");
				throw new Exception("Invalid side panel folder '" + selectedPath + "', select 'Assets/Mods/<your_mod_name>' or an individual file/folder in 'Assets/Mods/<your_mod_name>/Sources'");
			}
		}
		else if (!File.Exists(selectedPath) && !Directory.Exists(selectedPath))
		{
			throw new Exception("Selection does not exist: " + selectedPath);
		}
	}

	public static string ToValidKoiFileName(string input)
	{
		return Zipmod.ReplaceInvalidChars(input.ToLower().Replace(".", "_").Replace(" ", "_"));
	}

	public static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;

		string[] words = Regex.Split(ToValidKoiFileName(input).Replace("-", "_"), @"[\s_]+");
		StringBuilder pascalCase = new StringBuilder();

		foreach (string word in words)
		{
			if (word.Length > 0)
			{
				pascalCase.Append(char.ToUpper(word[0]));
				if (word.Length > 1)
				{
					pascalCase.Append(word.Substring(1).ToLower());
				}
			}
		}

		return pascalCase.ToString();
	}

	public static string ToSnakeCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;

		string snakeCase = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower().Replace("-", "_");
		return Regex.Replace(ToValidKoiFileName(snakeCase), @"_+", "_");
	}

	public static string GetCsvFolder(string modPath)
	{
		string[] listsPath = Directory.GetDirectories(Path.Combine(modPath, "List/Studio"));
		if (listsPath.Length == 0)
		{
			throw new Exception("Missing List/Studio/<mod_folder> folder in mod directory: " + modPath);
		}
		else if (listsPath.Length > 1)
		{
			throw new Exception("Multiple folders found in List/Studio of mod directory: " + modPath);
		}
		else
		{
			return listsPath[0];
		}
	}

	public static Tuple<string> GetCsvItemFilePaths(string modPath)
	{
		string csvFolder = GetCsvFolder(modPath);
		string[] listFiles = Directory.GetFiles(csvFolder, "ItemList_00*.csv");
		string[] categoryFiles = Directory.GetFiles(csvFolder, "ItemCategory_00*.csv");
		return Tuple<string>.Create(
			categoryFiles.Length > 0 ? categoryFiles[0] : null, 
			listFiles.Length > 0 ? listFiles[0] : null
		);
	}

	public static Tuple<string> GetCsvModInfo(string itemListPath)
	{
		Match match = Regex.Match(Path.GetFileName(itemListPath), @"ItemList_(\d+)_(\d+)_(\d+)");
		if (!match.Success)
		{
			return Tuple<string>.Create(null, null);
		}
		else
		{
			return Tuple<string>.Create(match.Groups[2].Value, match.Groups[3].Value);
		}
	}

	public static void WriteToCsv(string filePath, List<string> lines)
	{
		File.WriteAllLines(filePath, lines.ToArray(), System.Text.Encoding.UTF8);
	}

	public static List<string> GetEmptyItemGroupCsv()
	{
		return new List<string>(new string[] { "Group Number,Name" });
	}

	public static List<string> GetEmptyItemCategoryCsv()
	{
		return new List<string>(new string[] { "Category Number,Name" });
	}

	public static List<string> GetEmptyItemListCsv()
	{
		return new List<string> (new string[] {"ID,Group Number,Category Number,Name,Manifest,Bundle Path,File Name,Child Attachment Transform,Animation,Color 1,Pattern 1,Color 2,Pattern 2,Color 3,Pattern 3,Scaling,Emission"});
	}

	public static string GetModCsvPath(string modPath, string modName)
	{
		string[] listsPath = Directory.GetDirectories(Path.Combine(modPath, "List/Studio"));
		if (listsPath.Length == 0)
		{
			throw new Exception("Missing List/Studio/<mod_folder> folder in mod directory: " + modPath);
		}
		else if (listsPath.Length > 1)
		{
			throw new Exception("Multiple List/Studio folders found in mod directory: " + modPath);
		}
		else
		{
			return listsPath[0];
		}
	}

	public static string GetModUID(string modPath)
	{
		string manifestPath = Path.Combine(modPath, "manifest.xml");
		if (!File.Exists(manifestPath))
		{
			throw new Exception("Manifest file not found: " + manifestPath);
		}

		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.Load(manifestPath);

		XmlNode muidNode = xmlDoc.SelectSingleNode("//muid");
		if (muidNode == null)
		{
			throw new Exception("MUID node not found in manifest file: " + manifestPath);
		}

		return muidNode.InnerText;
	}

	public static string MakeModGuid(string authorName, string modName)
	{
		return "com." + authorName + "." + ToPascalCase(modName);
	}

	public static string GetModGuid(string modPath)
	{
		ManifestInfo manifestInfo = new ManifestInfo(Path.Combine(modPath, "manifest.xml"));
		if (string.IsNullOrEmpty(manifestInfo.guid))
		{
			throw new Exception("GUID not found in manifest file: " + Path.Combine(modPath, "manifest.xml"));
		}
		else
		{
			return manifestInfo.guid;
		}
	}

	public static string GetModPath(string selectedPath)
	{
		string[] directories = selectedPath.Split('/');
		for (int i = 0; i < directories.Length; i++)
		{
			if (directories[i] == "Mods" && i + 1 < directories.Length)
			{
				return string.Join(Path.DirectorySeparatorChar.ToString(), directories, 0, i + 2);
			}
		}

		throw new Exception("Mods folder not found in path: " + selectedPath);
	}

	public static string GetRelativePath(string basePath, string fullPath)
	{
		basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		fullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

		Uri baseUri = new Uri(basePath);
		Uri fullUri = new Uri(fullPath);
		return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
	}

	public static void SetAssetBundleNameInMetaFile(string metaFilePath, string bundleName)
	{
		string[] lines = File.ReadAllLines(metaFilePath);
		bool found = false;

		for (int i = 0; i < lines.Length; i++)
		{
			if (lines[i].StartsWith("  assetBundleName:"))
			{
				lines[i] = "  assetBundleName: " + bundleName;
				found = true;
				break;
			}
		}

		if (!found)
		{
			List<string> linesList = new List<string>(lines);
			linesList.Insert(linesList.Count - 1, "  assetBundleName: " + bundleName);
			lines = linesList.ToArray();
		}

		File.WriteAllLines(metaFilePath, lines);
	}

	public static SEComponent GetSEComponent(string prefabPath)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (prefab == null)
		{
			throw new Exception("Prefab not found at path: " + prefabPath);
		}
		else
		{
			return prefab.GetComponent<SEComponent>();
		}

	}

	public static bool IsPrefabAudioLoop(string prefabPath)
	{
		SEComponent seComponent = GetSEComponent(prefabPath);
		if (seComponent == null)
		{
			throw new Exception("Prefab does not have an SEComponent: " + prefabPath);
		}
		else
		{
			return seComponent._isLoop == true;
		}
	}
}

}