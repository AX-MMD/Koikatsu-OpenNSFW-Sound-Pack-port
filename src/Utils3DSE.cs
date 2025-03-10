using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Studio.Sound;
using IllusionMods.KoikatuModdingTools;

namespace IllusionMods.Koikatsu3DSEModTools {

public static class Utils
{
	public struct GenerationResult
	{
		public int createCount;
		public int updateCount;
		public int deleteCount;
	}

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

		public void Update<T>(T fields) where T : ManifestInfo
		{
			this.Update(
				fields.guid,
				fields.author,
				fields.name,
				fields.muid,
				fields.version,
				fields.description,
				fields.website
			);
		}

		public void Update(Dictionary<string, string> fields)
		{
			this.Update(
				fields.ContainsKey("guid") ? fields["guid"] : null,
				fields.ContainsKey("author") ? fields["author"] : null,
				fields.ContainsKey("name") ? fields["name"] : null,
				fields.ContainsKey("muid") ? fields["muid"] : null,
				fields.ContainsKey("version") ? fields["version"] : null,
				fields.ContainsKey("description") ? fields["description"] : null,
				fields.ContainsKey("website") ? fields["website"] : null
			);
		}

		public void Update(string guid = null, string author = null, string name = null, string muid = null, string version = null, string description = null, string website = null)
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

		public Dictionary<string, string> ToDictionary()
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

		public List<string> Validate()
		{
			List<string> errors = new List<string>();
			if (string.IsNullOrEmpty(this.guid))
			{
				errors.Add("GUID is required");
			}
			if (string.IsNullOrEmpty(this.author))
			{
				errors.Add("Author name is required");
			}
			if (string.IsNullOrEmpty(this.name))
			{
				errors.Add("Mod name is required");
			}
			if (string.IsNullOrEmpty(this.muid))
			{
				errors.Add("MUID is required");
			}
			else if (!Regex.IsMatch(this.muid, @"^\d{3,6}$"))
			{
				errors.Add("MUID must be a 3-6 digit number");
			}
			else if (int.Parse(this.muid) < 100 || int.Parse(this.muid) > 999999)
			{
				errors.Add("MUID must be between 100 and 999999");
			}
			return errors;
		}

		public void Save(string manifestPath)
		{
			if (string.IsNullOrEmpty(manifestPath))
			{
				throw new Exception("Manifest filePath is required.");
			}

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(manifestPath);

			foreach (KeyValuePair<string, string> field in this.ToDictionary())
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

			List<string> errors = this.Validate();
			if (errors.Count == 0)
			{
				xmlDoc.Save(manifestPath);
			}
			else
			{
				throw new Exception(string.Join("\n", errors.ToArray()));
			}
		}

		public static bool operator ==(ManifestInfo m1, ManifestInfo m2)
		{
			if (ReferenceEquals(m1, m2))
			{
				return true;
			}

			if ((object)m1 == null || (object)m2 == null)
			{
				return false;
			}

			return m1.guid == m2.guid &&
				m1.author == m2.author &&
				m1.name == m2.name &&
				m1.muid == m2.muid &&
				m1.version == m2.version &&
				m1.description == m2.description &&
				m1.website == m2.website;
		}

		public static bool operator !=(ManifestInfo m1, ManifestInfo m2)
		{
			return !(m1 == m2);
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			ManifestInfo other = (ManifestInfo)obj;
			return this == other;
		}

		public override int GetHashCode()
		{
			return (guid ?? "").GetHashCode() ^
				(author ?? "").GetHashCode() ^
				(name ?? "").GetHashCode() ^
				(muid ?? "").GetHashCode() ^
				(version ?? "").GetHashCode() ^
				(description ?? "").GetHashCode() ^
				(website ?? "").GetHashCode();
		}
	}

	public static string GetAssetBundlePath(string modPath, string categoryKey)
	{
		return "studio/" + ToZipmodFileName(GetModGuid(modPath)) + "/" + Utils.ToZipmodFileName(categoryKey) + "/bundle.unity3d";
	}

	public static bool IsValid3DSEModPath(string path)
	{
		return Directory.Exists(path) && Directory.GetParent(path).Name == "Mods" && File.Exists(Path.Combine(path, "base_3dse.prefab"));
	}

	public static string ToZipmodFileName(string input)
	{
		return Zipmod.ReplaceInvalidChars(input.ToLower().Replace(".", "_").Replace(" ", "_"));
	}

	public static T GetLastValue<T>(IEnumerable<T> collection)
	{
		return collection.Last();
	}

	public static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;

		string[] words = Regex.Split(input.Replace("-", "_"), @"[\s_]+");
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

	public static string ToItemCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		 
		string[] words = Regex.Split(Zipmod.ReplaceInvalidChars(input.Replace(".", "_").Replace(" ", "_")), @"[\s_]+");
		StringBuilder itemCase = new StringBuilder();

		foreach (string word in words)
		{
			foreach (string dashWord in word.Split('-'))
			{
				if (dashWord.Length > 0)
				{
					itemCase.Append(char.ToUpper(dashWord[0]));
					if (dashWord.Length > 1)
					{
						itemCase.Append(dashWord.Substring(1));
					}
				}
			}
		}

		return itemCase.ToString();
	}

	public static string ToSnakeCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;

		string snakeCase = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower().Replace("-", "_");
		return Regex.Replace(ToZipmodFileName(snakeCase), @"_+", "_");
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
		return "com." + Zipmod.ReplaceInvalidChars(authorName) + "." + Zipmod.ReplaceInvalidChars(modName.Replace(" ", "-"));
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

	public static string GetModPath(string selectedPath, bool raise_exec = false)
	{
		// Get the directory separator character from selectedPath
		string[] directories = selectedPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
		for (int i = 0; i < directories.Length; i++)
		{
			if (directories[i] == "Mods" && i + 1 < directories.Length)
			{
				return string.Join(Path.DirectorySeparatorChar.ToString(), directories, 0, i + 2);
			}
		}

		if (raise_exec)
		{
			throw new Exception("Mods folder not found in path: " + selectedPath);
		}
		else
		{
			return null;
		}
	}

	public static string GetModName(string modPath)
	{
		if (Directory.GetParent(modPath).Name != "Mods")
		{
			modPath = GetModPath(modPath, true);
		}
		string[] directories = modPath.Split(Path.DirectorySeparatorChar);
		return directories[directories.Length - 1];
	}

	public static string GetRelativePath(string basePath, string fullPath)
	{
		basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		fullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

		Uri baseUri = new Uri(basePath);
		Uri fullUri = new Uri(fullPath);
		return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
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

	public static string FormatErrorWithStackTrace(Exception e)
	{
		return string.Format("{0}: {1}\nStack Trace:\n{2}", e.GetType().Name, e.Message, e.StackTrace);
	}
	
	public static void LogErrorWithTrace(Exception e)
	{
		Debug.LogError(FormatErrorWithStackTrace(e));
	}

	public static void FileMove(string sourcePath, string destPath)
	{
		File.Move(Path.GetFullPath(sourcePath), Path.GetFullPath(destPath));
		if (File.Exists(sourcePath + ".meta"))
		{
			File.Move(Path.GetFullPath(sourcePath + ".meta"), Path.GetFullPath(destPath + ".meta"));
		}
	}

	public static void FileReplace(string sourcePath, string destPath)
	{
		File.Delete(Path.GetFullPath(destPath));
		FileMove(sourcePath, destPath);
	}

	public static void FileDelete(string path)
	{
		File.Delete(Path.GetFullPath(path));
		if (File.Exists(path + ".meta"))
		{
			File.Delete(Path.GetFullPath(path + ".meta"));
		}
	}

	public static string FullPathToAssetPath(string fullPath)
    {
        // Ensure the path uses forward slashes
        fullPath = fullPath.Replace("\\", "/");

        // Find the index of the "Assets" folder in the full path
        int index = fullPath.IndexOf("Assets");

        if (index < 0)
        {
            Debug.LogError("The path does not contain an 'Assets' folder: " + fullPath);
            return null;
        }

        // Return the substring starting from the "Assets" folder
        return fullPath.Substring(index);
    }
	
}

}