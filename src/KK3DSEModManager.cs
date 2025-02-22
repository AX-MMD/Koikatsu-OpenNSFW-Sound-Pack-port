using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Studio.Sound;
using System.Text;

public class KK3DSEModManager
{
    public string modPath;
    public string modName;
    public string prefabOutputPath;
    public string sourcesPath;
    public string basePrefabPath;
    public UnityEngine.Object basePrefab;
    public string csvFolder;

    public KK3DSEModManager(string selectedPath)
    {
        this.modPath = Utils.GetModPath(selectedPath);
        this.modName = Utils.GetModName(selectedPath);
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
        this.basePrefab = AssetDatabase.LoadAssetAtPath(this.basePrefabPath, typeof(GameObject));
        if (this.basePrefab == null)
        {
            throw new Exception("Base prefab not found at path: " + this.basePrefabPath);
        }
        this.csvFolder = this.getCsvFolder();
    }

    public List<Category> getCategories()
    {
        List<Category> categories = new List<Category>();
        foreach (string rootFolder in Directory.GetDirectories(this.sourcesPath))
        {
            string rootFolderName = Path.GetFileName(rootFolder);
            categories.Add(new Category("[" + rootFolderName + "]"));
            foreach (string subfolder in Directory.GetDirectories(rootFolder))
            {
                categories.AddRange(this.recursiveGetCategories(subfolder, new List<string>()));
            }
        }
        return categories;
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

        int prefabCount = 0;

        foreach (Category category in categories)
        {
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

            foreach (WavFile file in category.files)
            {
                string newPrefabName = string.Format("{0}.prefab", file.prefabName);
                string newPrefabPath = Path.Combine(categoryOutputPath, newPrefabName).Replace("\\", "/");

                GameObject newObject = null;

                if (!create && File.Exists(newPrefabPath))
                {
                    //update the existing prefab
                    Debug.Log("Updating prefab: " + newPrefabPath);
                    newObject = (GameObject)AssetDatabase.LoadAssetAtPath(newPrefabPath, typeof(GameObject));
                }
                else
                {
                    //create a new prefab, if there is already a prefab with the same name, it will be overwritten
                    Debug.Log("Creating prefab: " + newPrefabPath);
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

                Debug.Log("Prefab created: " + newPrefabPath);

                prefabCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log(string.Format("{0} Prefabs generated successfully.", prefabCount));
        }
    }

    public void generateCSV(bool create, bool isSidePanel, List<Category> categories)
    {
        string itemListPath;
        string categoryPath;
        string[] listFiles = Directory.GetFiles(csvFolder, "ItemList_*.csv");
        string[] categoryFiles = Directory.GetFiles(csvFolder, "ItemCategory_*.csv");
        if (categoryFiles.Length == 0)
        {
            categoryPath = Path.Combine(csvFolder, "ItemCategory_00_YYY.csv");
        }
        else
        {
            categoryPath = categoryFiles[0];
        }
        if (listFiles.Length == 0)
        {
            itemListPath = Path.Combine(csvFolder, "ItemList_00_11_YYY.csv");
        }
        else
        {
            itemListPath = listFiles[0];
        }

        Dictionary<string, string[]> existingCategories = new Dictionary<string, string[]>();
        List<string> categoryLines = new List<string>();
        if (create && isSidePanel || !File.Exists(categoryPath))
        {
            categoryLines.Add("Category Number,Name");
        }
        else
        {
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
        List<string> itemListLines = new List<string>();
        if (create && isSidePanel || !File.Exists(itemListPath))
        {
            itemListLines.Add("ID,Group Number,Category Number,Name,Manifest,Bundle Path,File Name,Child Attachment Transform,Animation,Color 1,Pattern 1,Color 2,Pattern 2,Color 3,Pattern 3,Scaling,Emission");
        }
        else
        {
            string[] lines = File.ReadAllLines(itemListPath);
            foreach (string line in lines)
            {
                itemListLines.Add(line);
                string[] columns = line.Split(',');
                if (columns.Length > 6)
                {
                    existingEntries[columns[6].Trim()] = columns;
                }
            }
        }

        // Extract the category number from the digits in the file name
        Match match = Regex.Match(Path.GetFileName(itemListPath), @"ItemList_(\d{2,10})_(\d+)_(\d+)");
        if (!match.Success)
        {
            throw new Exception("Invalid CSV file name: " + itemListPath);
        }

        int id;
        if (existingEntries.Count == 0 || create && isSidePanel)
        {
            id = 1;
        }
        else
        {
            id = int.Parse(this.getLastValue(existingEntries.Values)[0]) + 1;
        }
        string groupNumber = match.Groups[2].Value;
        string categoryNumber;
        if (existingCategories.Count == 0 || create && isSidePanel)
        {
            categoryNumber = match.Groups[3].Value;
        }
        else
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

            foreach (WavFile file in category.files)
            {
                string bundlePath = this.getAssetBundlePath();
                string itemName = file.itemName;
                if (create && isSidePanel || !existingEntries.ContainsKey(file.prefabName))
                {
                    // Add new entry
                    string[] newEntry = new string[] { id.ToString(), groupNumber, currentCategoryNumber, itemName, "", bundlePath, file.prefabName, "", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE" };
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
                    itemListLines[itemListLines.IndexOf(string.Join(",", existingEntry))] = string.Join(",", existingEntry);
                }
            }
        }

        File.WriteAllLines(categoryPath, categoryLines.ToArray(), System.Text.Encoding.UTF8);
        File.WriteAllLines(itemListPath, itemListLines.ToArray(), System.Text.Encoding.UTF8);
        Debug.Log("CSV files generated successfully.");
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

    private string toPrefabName(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string snakeCase = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower();
        snakeCase = Regex.Replace(snakeCase, @"[\s!@#$%^&*()\-_=+\[\]{};:'"",<.>/?\\|~`]", "_");
        snakeCase = Regex.Replace(snakeCase, @"_+", "_");
        return snakeCase;
    }

    private string toItemName(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string[] words = Regex.Split(this.toPrefabName(input), @"[\s_]+");
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

    private List<Category> recursiveGetCategories(string folder, List<string> folderTags)
    {
        string folderName = Path.GetFileName(folder);
        Match match = Regex.Match(folderName, @"^(?<categoryName>[^()]+)(\((?<author>[^)]+)\))?(\[(?<tags>[^\]]+)\])*$");
        string categoryName = match.Groups["categoryName"].Value.Trim();
        string author = match.Groups["author"].Value.Trim();
        List<string> tags = new List<string>();

        // Capture all tags within square brackets
        foreach (Capture capture in match.Groups["tags"].Captures)
        {
            tags.AddRange(capture.Value.Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries));
        }

        List<string> currentTags = new List<string>(folderTags);
        currentTags.AddRange(tags);
        List<Category> categories = new List<Category>();

        if (!string.IsNullOrEmpty(author))
        {
            if (currentTags.Contains("skip-folder-name"))
            {
                currentTags.Remove("skip-folder-name");
                categoryName = "";
            }

            categories.Add(new Category(categoryName, author, this.getCategoryFiles(folder, currentTags, this.toItemName(categoryName))));
            return categories;
        }
        else
        {
            currentTags.Remove("skip-folder-name");
            foreach (string subfolder in Directory.GetDirectories(folder))
            {
                categories.AddRange(this.recursiveGetCategories(subfolder, currentTags));
            }
        }

        return categories;
    }

    private List<WavFile> getCategoryFiles(string folder, List<string> folderTags, string pathName = "")
    {
        List<WavFile> files = new List<WavFile>();
        foreach (string entry in Directory.GetFileSystemEntries(folder))
        {
            if (Directory.Exists(entry))
            {
                string itemName = Path.GetFileName(entry);
                Match match = Regex.Match(itemName, @"^(?<folderName>[^[]+)(\[(?<tags>[^\]]+)\])*$");
                string folderName = match.Groups["folderName"].Value.Trim();
                List<string> tags = new List<string>();

                // Capture all tags within square brackets
                foreach (Capture capture in match.Groups["tags"].Captures)
                {
                    tags.AddRange(capture.Value.Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries));
                }

                List<string> currentTags = new List<string>(folderTags);
                currentTags.AddRange(tags);

                if (folderName.ToUpper().Contains("FX") || folderName.ToUpper() == "ORIGINAL")
                {
                    continue;
                }
                else if (folderName.ToUpper() == "NORMAL")
                {
                    files.AddRange(this.getCategoryFiles(entry, folderTags, pathName));
                }
                else if (currentTags.Contains("skip-folder-name"))
                {
                    tags.Remove("skip-folder-name");
                    files.AddRange(this.getCategoryFiles(entry, currentTags, pathName));
                }
                else
                {
                    files.AddRange(this.getCategoryFiles(entry, currentTags, pathName + this.toItemName(folderName)));
                }
            }
            else if (entry.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                string itemName = this.buildFilename(pathName, folderTags, entry, files.Count + 1);
                files.Add(new WavFile(this.toPrefabName(itemName), itemName, entry, folderTags.Contains("loop")));
            }
        }
        return files;
    }

    private string buildFilename(string pathName, List<string> tags, string filename, int index)
    {
        if (tags.Contains("keep-name"))
        {
            return Path.GetFileNameWithoutExtension(filename);
        }

        string name = pathName;
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

        return name + (index > 9 ? index.ToString() : "0" + index.ToString());
    }

    private string getCsvFolder()
    {
        string[] listsPath = Directory.GetDirectories(Path.Combine(this.modPath, "List/Studio"));
        if (listsPath.Length == 0)
        {
            throw new Exception("Missing List/Studio/<mod_folder> folder in mod directory: " + this.modPath);
        }
        else if (listsPath.Length > 1)
        {
            throw new Exception("Multiple List/Studio folders found in mod directory: " + this.modPath);
        }
        else
        {
            return listsPath[0];
        }
    }

    private string getAssetBundlePath()
    {
        return "studio/" + this.modName + "/bundle.unity3d";
    }
}