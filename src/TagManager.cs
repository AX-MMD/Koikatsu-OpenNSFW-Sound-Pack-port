using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace IllusionMods.Koikatsu3DSEModTools {

	public static class TagManager {

		public static HashSet<string> ValidTags = new HashSet<string> { 
			// type "tag%%<value>" tags //
			"append", 
			"prepend",
			// "volume",
			// "threshold",
			// regular tags //
			"appendfilename", 
			"prependfilename", 
			"no-index", 
			"indexed", 
			"loop", 
			"keep-name", 
			"format-keep-name",
			"skip-folder-name"
		};
		public static string FileExtention = ".3dsetags";

		public class ValidationError : Exception
		{
			public ValidationError(string message) : base(message) { }
		}

		public static List<string> GetTags(string folderPath, ICollection<string> cumulTags = null, bool useLegacy = false)
		{
			return cumulTags == null ? LoadTags(folderPath) : CombineTags(cumulTags, LoadTags(folderPath), useLegacy);
		}

		public static List<string> LoadTags(string folderPath)
		{
			List<string> tags = new List<string>();
			foreach (string file in Directory.GetFiles(folderPath, "*" + FileExtention))
			{
				MatchCollection matches = Regex.Matches(Path.GetFileName(file), @"\[(?<tags>[^\]]+)\]");
				foreach (Match match in matches)
				{
					tags.Add(match.Groups["tags"].Value);
				}
			}
			return tags;
		}

		public static List<string> CombineTags(ICollection<string> tags1, ICollection<string> tags2, bool useLegacyClassifier = false)
		{
			List<string> combinedTags = new List<string>(tags1);
			if (tags2.Contains("no-index") || useLegacyClassifier && tags2.Contains("loop"))
			{
				combinedTags.RemoveAll(item => item == "indexed");
			}
			if (tags2.Contains("indexed"))
			{
				combinedTags.RemoveAll(item => item == "no-index");
			}
			if (tags2.Contains("keep-name"))
			{
				combinedTags.RemoveAll(item => item == "format-keep-name");
				combinedTags.RemoveAll(item => item == "indexed");
			}
			if (tags2.Contains("format-keep-name"))
			{
				combinedTags.RemoveAll(item => item == "keep-name");
				combinedTags.RemoveAll(item => item == "indexed");
			}
			combinedTags.AddRange(tags2);
			return combinedTags;
		}

		public static void EditTags(string folderPath, IEnumerable<string> tags)
		{
			EditTags(folderPath, "[" + string.Join("][", tags.ToArray()) + "]");
		}

		public static void EditTags(string folderPath, string tagsInput)
		{
			if (string.IsNullOrEmpty(tagsInput) || tagsInput == "[]")
			{
				foreach (string file in Directory.GetFiles(folderPath, "*" + FileExtention))
				{
					File.Delete(file);
					if (File.Exists(file + ".meta"))
					{
						File.Delete(file + ".meta");
					}
				}
			}
			else 
			{
				if (!Regex.IsMatch(tagsInput, @"^\[.*\]$"))
				{
					throw new ValidationError("Tags must be enclosed in brackets.");
				}
				else if (!IsValidTagsString(tagsInput))
				{
					throw new ValidationError(string.Format("Invalid tags {0}, valid tags are: {1}", tagsInput, string.Join(", ", new List<string>(ValidTags).ToArray())));
				}

				string tagFilePath = Path.Combine(folderPath, tagsInput + FileExtention);
				if (!File.Exists(tagFilePath))
				{
					File.Create(tagFilePath).Close();
				}
				foreach (string file in Directory.GetFiles(folderPath, "*" + FileExtention))
				{
					if (file != tagFilePath)
					{
						File.Delete(file);
						if (File.Exists(file + ".meta"))
						{
							File.Delete(file + ".meta");
						}
					}
				}
			}

			AssetDatabase.Refresh();
		}

		public static bool IsValidTag(string tag)
		{
			return ValidTags.Contains(tag) || ValidTags.Contains(tag.Split(new string[] { "%%" }, StringSplitOptions.None)[0]);
		}

		public static bool IsValidTags(IEnumerable<string> tags)
		{
			foreach (string tag in tags)
			{
				if (!IsValidTag(tag))
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsValidTagsString(string tagsString)
		{
			if (!Regex.IsMatch(tagsString, @"^\[.*\]$"))
			{
				return false;
			}

			return IsValidTags(tagsString.Substring(1, tagsString.Length - 2).Split(new string[] { "][" }, StringSplitOptions.None));
		}

		public static string ApplyNameModifierTags(string itemName, ICollection<string> tags, string filename, int index)
		{
			if (tags.Contains("keep-name"))
			{
				return tags.Contains("no-index") ? Path.GetFileNameWithoutExtension(filename) : Path.GetFileNameWithoutExtension(filename) + index.ToString("D2");
			}
			if (tags.Contains("format-keep-name"))
			{
				if (tags.Contains("no-index"))
				{
					return Utils.ToItemCase(Path.GetFileNameWithoutExtension(filename));
				}
				else
				{
					return Utils.ToItemCase(Path.GetFileNameWithoutExtension(filename)) + index.ToString("D2");
				}
			}

			// Iterate in reverse, deeper tags should be applied first
			string name = itemName;
			string[] tagsArray = tags.ToArray();
			for (int i = tagsArray.Count() - 1; i >= 0; i--)
			{
				string tag = tagsArray[i];
				Match appendMatch = Regex.Match(tag, @"append%%(?<appendValue>.+)");
				if (appendMatch.Success)
				{
					name += appendMatch.Groups["appendValue"].Value;
				}

				Match prependMatch = Regex.Match(tag, @"prepend%%(?<prependValue>.+)");
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

			return tags.Contains("no-index") ? name : name + index.ToString("D2");
		}

		public static PrefabModifier GetPrefabModifier(ICollection<string> tags)
		{
			bool isLoop = tags.Contains("loop");
			float volume = -1.0f;
			Utils.Tuple<float> threshold = null;

			// Deprecated volume and threshold tags in favor of editing prefabs directly and use UpdateFromSource

			// foreach (string tag in tags)
			// {
			//     Match volumeMatch = Regex.Match(tag, @"volume%%(?<volumeValue>.+)");
			//     if (volumeMatch.Success)
			//     {
			//         volume = float.Parse(volumeMatch.Groups["volumeValue"].Value);
			//     }

			//     Match thresholdMatch = Regex.Match(tag, @"threshold%%(?<minValue>\d+(\.\d+)?)-(?<maxValue>\d+(\.\d+)?)");
			//         if (thresholdMatch.Success)
			//         {
			//             threshold = new Utils.Tuple<float>(float.Parse(thresholdMatch.Groups["minValue"].Value), float.Parse(thresholdMatch.Groups["maxValue"].Value));
			//             break;
			//         }
			// }
			return new PrefabModifier(isLoop, threshold, volume);
		}
	}
}