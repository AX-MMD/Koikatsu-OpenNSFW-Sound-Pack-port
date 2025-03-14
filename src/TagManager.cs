using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IllusionMods.Koikatsu3DSECategoryTool;


namespace IllusionMods.Koikatsu3DSEModTools {

	public static class TagManager {

		public static class Tags {
			// tag%%value kind of tags
			public const string Append = "append";
			public const string Prepend = "prepend";
			// regular tags
			public const string AppendFilename = "appendfilename";
			public const string PrependFilename = "prependfilename";
			public const string Indexed = "indexed";
			public const string Loop = "loop";
			public const string KeepName = "keep-name";
			public const string FormatKeepName = "format-keep-name";
			// negation tags
			public const string NoIndex = "no-indexed";
			public const string NoLoop = "no-loop";
			public const string NoKeepName = "no-keep-name";
			public const string Reset = "reset";
			// folder action tags
			public const string SkipFolderName = "skip-folder-name";
			public const string LegacyClassifier = "legacy-classifier";

			public static HashSet<string> ToHashSet() {
				// add every fields in this class except for the ToHashSet method
				return new HashSet<string>(typeof(Tags)
					.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
					.Where(f => f.FieldType == typeof(string))
					.Select(f => (string)f.GetValue(null)));
			}
		}

		public static HashSet<string> ValidTags = Tags.ToHashSet();
		public const string FileExtention = ".3dsetags";

		public class ValidationError : Exception
		{
			public ValidationError(string message) : base(message) { }
		}

		public static List<string> GetTags(string folderPath, ICollection<string> cumulTags = null)
		{
			return cumulTags == null ? LoadTags(folderPath) : CombineTags(cumulTags, LoadTags(folderPath));
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

		public static List<string> CombineTags(ICollection<string> tags1, ICollection<string> tags2)
		{
			List<string> combinedTags = new List<string>(tags1);			
			if (tags1.Contains(Tags.LegacyClassifier) && tags2.Contains(Tags.Loop))
			{
				combinedTags.RemoveAll(item => item == Tags.Indexed);
			}

			foreach (string tag in tags2)
			{
				if (tag == Tags.Reset)
				{
					combinedTags = new List<string>();
				}
				else if (tag == Tags.NoIndex)
				{
					combinedTags.RemoveAll(item => item == Tags.Indexed);
				}
				else if (tag == Tags.NoLoop)
				{
					combinedTags.RemoveAll(item => item == Tags.Loop);
				}
				else if (tag == Tags.NoKeepName)
				{
					combinedTags.RemoveAll(item => item == Tags.KeepName || item == Tags.FormatKeepName);
				}
				else if (tag == Tags.KeepName)
				{
					combinedTags.RemoveAll(item => item == Tags.FormatKeepName || item == Tags.Indexed);
					combinedTags.Add(tag);
				}
				else if (tag == Tags.FormatKeepName)
				{
					combinedTags.RemoveAll(item => item == Tags.KeepName || item == Tags.Indexed);
					combinedTags.Add(tag);
				}
				else
				{
					combinedTags.Add(tag);
				}
			}

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
			if (tags.Contains(Tags.KeepName))
			{
				return Path.GetFileNameWithoutExtension(filename) + (tags.Contains(Tags.Indexed) ? index.ToString("D2") : "");
			}
			if (tags.Contains(Tags.FormatKeepName))
			{
				return Utils.ToItemCase(Path.GetFileNameWithoutExtension(filename)) + (tags.Contains(Tags.Indexed) ? index.ToString("D2") : "");
			}

			// Iterate in reverse, deeper tags should be applied first
			string name = itemName;
			string[] tagsArray = tags.ToArray();
			for (int i = tagsArray.Count() - 1; i >= 0; i--)
			{
				string tag = tagsArray[i];
				Match appendMatch = Regex.Match(tag, @"" + Tags.Append + "%%(?<appendValue>.+)");
				if (appendMatch.Success)
				{
					name += appendMatch.Groups["appendValue"].Value;
				}

				Match prependMatch = Regex.Match(tag, @"" + Tags.Prepend + "%%(?<prependValue>.+)");
				if (prependMatch.Success)
				{
					name = prependMatch.Groups["prependValue"].Value + name;
				}
			}

			if (tags.Contains(Tags.AppendFilename))
			{
				name += Path.GetFileNameWithoutExtension(filename);
			}
			if (tags.Contains(Tags.PrependFilename))
			{
				name = Path.GetFileNameWithoutExtension(filename) + name;
			}

			return tags.Contains(Tags.Indexed) ? name + index.ToString("D2") : name;
		}

		public static PrefabModifier GetPrefabModifier(ICollection<string> tags)
		{
			bool isLoop = tags.Contains(Tags.Loop);
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