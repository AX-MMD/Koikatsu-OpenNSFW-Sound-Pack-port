using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class GenerateItemListFromPrefabs : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate Only ItemList")]
    public static void GenerateCSV()
    {
        GenerateCSV(false);
    }

    public static void GenerateCSV(bool create)
    {
        Debug.Log("Generate ItemList From Prefabs started...");
        string selectedPath;
        bool isSidePanel;
        Utils.GetSelectedFolderPath(out selectedPath, out isSidePanel);

        string modPath = Utils.GetModPath(selectedPath);
        string[] listsPath = Directory.GetDirectories(Path.Combine(modPath, "List/Studio"));
        string outputPath;
        
        if (listsPath.Length == 0)
        {
            outputPath = Path.Combine(modPath, "List/Studio/" + Utils.ToSnakeCase(Utils.GetModName(modPath)) + "/ItemList_00_11_YYY.csv");
        }
        else
        {
            string[] csvFiles = Directory.GetFiles(listsPath[0], "ItemList_*.csv");
            if (csvFiles.Length == 0)
            {
                outputPath = Path.Combine(listsPath[0], "ItemList_00_11_YYY.csv");
            }
            else
            {
                outputPath = csvFiles[0];
            }
        }

        string prefabFolderPath = Path.Combine(modPath, "Prefab");
        if (!Directory.Exists(prefabFolderPath))
        {
            Directory.CreateDirectory(prefabFolderPath);
        }

        Dictionary<string, string[]> existingEntries = new Dictionary<string, string[]>();
        List<string> csvLines = new List<string>();
        if (create && isSidePanel || !File.Exists(outputPath))
        {
            csvLines.Add("ID,Group Number,Category Number,Name,Manifest,Bundle Path,File Name,Child Attachment Transform,Animation,Color 1,Pattern 1,Color 2,Pattern 2,Color 3,Pattern 3,Scaling,Emission");
        }
        else
        {
            string[] lines = File.ReadAllLines(outputPath);
            foreach (string line in lines)
            {
                csvLines.Add(line);
                string[] columns = line.Split(',');
                if (columns.Length > 6)
                {
                    existingEntries[columns[6].Trim()] = columns;
                }
            }
        }

        string[] prefabFiles = Directory.GetFiles(prefabFolderPath, "*.prefab");
        int id = csvLines.Count;

        //extract the category number from the digits in the file name
        Match match = Regex.Match(Path.GetFileName(outputPath), @"ItemList_(\d{2,10})_(\d+)_(\d+)");
        if (!match.Success)
        {
            throw new Exception("Invalid CSV file name: " + outputPath);
        }
        string groupNumber = match.Groups[2].Value;
        string categoryNumber = match.Groups[3].Value;

        foreach (string prefabFile in prefabFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabFile);
            string pascalCaseName = Utils.ToPascalCase(fileName);
            if (!Utils.IsPrefabAudioLoop(prefabFile))
            {
                pascalCaseName = "(S)" + pascalCaseName;
            }
            string bundlePath = AssetDatabase.GetImplicitAssetBundleName(prefabFile);
            if (string.IsNullOrEmpty(bundlePath))
            {
                throw new Exception("Prefab " + fileName + " does not have an asset bundle name.");
            }

            if (create && isSidePanel || !existingEntries.ContainsKey(fileName))
            {
                // Add new entry
                string[] newEntry = new string[] { id.ToString(), groupNumber, categoryNumber, pascalCaseName, "", bundlePath, fileName, "", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE" };
                csvLines.Add(string.Join(",", newEntry));
                id++;
            }
            else
            {
                // Update existing entry
                string[] existingEntry = existingEntries[fileName];
                existingEntry[3] = pascalCaseName; // Update Name
                existingEntry[5] = bundlePath; // Update Bundle Path
                existingEntry[6] = fileName; // Update File Name
                csvLines[csvLines.IndexOf(string.Join(",", existingEntry))] = string.Join(",", existingEntry);
            }
        }

        File.WriteAllLines(outputPath, csvLines.ToArray(), Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log("CSV file generated successfully at: " + outputPath);
    }
}