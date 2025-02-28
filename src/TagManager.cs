using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using UnityEditor;


namespace IllusionMods.Koikatsu3DSEModTools {

    public static class TagManager {

        public static HashSet<string> ValidTags = new HashSet<string> { 
            // type "tag%%<value>" tags
            "append", 
            "prepend", 
            "threshold", 
            "volume",
            // regular tags
            "appendfilename", 
            "prependfilename", 
            "no-index", 
            "indexed", 
            "loop", 
            "keep-name", 
            "skip-folder-name"
        };
        public static string FileExtention = ".3dsetags";

        public class ValidationError : Exception
        {
            public ValidationError(string message) : base(message) { }
        }

        public static List<string> GetTags(string folderPath)
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

        public static void EditTags(string folderPath, List<string> tags)
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

		public static List<string> CombineTags(List<string> tags1, List<string> tags2, bool useLegacyClassifier = false)
		{
			List<string> combinedTags = new List<string>(tags1);
			if (useLegacyClassifier && tags2.Contains("loop"))
			{
				combinedTags.Add("no-index");
			}
			if (tags2.Contains("no-index"))
			{
				combinedTags.Remove("indexed");
			}
			if (tags2.Contains("indexed"))
			{
				combinedTags.Remove("no-index");
			}
            combinedTags.AddRange(tags2);
			return combinedTags;
		}

        public static bool IsValidTag(string tag)
        {
            return ValidTags.Contains(tag) || ValidTags.Contains(tag.Split(new string[] { "%%" }, StringSplitOptions.None)[0]) || ValidTags.Contains(tag.Split('-')[0]);
        }

        public static bool IsValidTags(List<string> tags)
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
            
            return IsValidTags(new List<string>(tagsString.Substring(1, tagsString.Length - 2).Split(new string[] { "][" }, StringSplitOptions.None)));
        }

        public static string ApplyNameModifierTags(string itemName, List<string> tags, string filename, int index)
        {
            if (tags.Contains("keep-name"))
			{
				return filename;
			}

            string name = itemName;
            for (int i = tags.Count - 1; i >= 0; i--)
			{
				string tag = tags[i];
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

			if (tags.Contains("no-index"))
			{
				return name;
			}
			else
			{
				return name + (index > 9 ? index.ToString() : "0" + index.ToString());
			}
        }

        public static PrefabModifier GetPrefabModifier(List<string> tags)
        {
            bool isLoop = tags.Contains("loop");
            float volume = -1.0f;
            Utils.Tuple<float> threshold = null;
            foreach (string tag in tags)
            {
                Match volumeMatch = Regex.Match(tag, @"volume%%(?<volumeValue>.+)");
                if (volumeMatch.Success)
                {
                    volume = float.Parse(volumeMatch.Groups["volumeValue"].Value);
                }

                Match thresholdMatch = Regex.Match(tag, @"threshold%%(?<minValue>\d+(\.\d+)?)-(?<maxValue>\d+(\.\d+)?)");
                    if (thresholdMatch.Success)
                    {
                        threshold = new Utils.Tuple<float>(float.Parse(thresholdMatch.Groups["minValue"].Value), float.Parse(thresholdMatch.Groups["maxValue"].Value));
                        break;
                    }
            }
            return new PrefabModifier(isLoop, threshold, volume);
        }

        public static void ConvertTagsToNewVersion(string directoryPath)
        {
            List<string> newTags = new List<string>();
            foreach (string tag in GetTags(directoryPath))
            {
                if (!ValidTags.Contains(tag) && ValidTags.Contains(tag.Split('-')[0]))
                {
                    int pos = tag.IndexOf("-");
                    newTags.Add(tag.Substring(0, pos) + "%%" + tag.Substring(pos + 1));
                }
                else
                {
                    newTags.Add(tag);
                }
            }

            EditTags(directoryPath, newTags);
        }
    }
}