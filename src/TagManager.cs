using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using UnityEditor;


namespace IllusionMods.Koikatsu3DSEModTools {

    public static class TagManager {

        public static HashSet<string> ValidTags = new HashSet<string> { "append", "prepend", "threshold", "appendfilename", "prependfilename", "no-index", "indexed", "loop", "keep-name", "skip-folder-name" };
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

        public static void EditTags(string folderPath, string tagsInput)
        {
            if (string.IsNullOrEmpty(tagsInput))
            {
                throw new ValidationError("Please enter tags.");
            }
            else if (!Regex.IsMatch(tagsInput, @"^\[.*\]$"))
            {
                throw new ValidationError("Tags must be enclosed in brackets.");
            }
            else if (!IsValidTagsString(tagsInput))
            {
				throw new ValidationError("Invalid Valid tags are: " + string.Join(", ", new List<string>(ValidTags).ToArray()));
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
            return ValidTags.Contains(tag) || ValidTags.Contains(tag.Split('-')[0]);
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
    }
}