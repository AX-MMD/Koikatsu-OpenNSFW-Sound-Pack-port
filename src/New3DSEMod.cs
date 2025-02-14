using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

public class New3DSEMod : MonoBehaviour
{
    [MenuItem("Assets/3DSE/New 3DSE Mod")]
    public static void CopyAndRename()
    {
        string sourcePath = "Assets/Examples/Studio 3DSE Example";
        string destinationPath = "Assets/Mods";

        if (!Directory.Exists(sourcePath))
        {
            Debug.LogError("Source path does not exist: " + sourcePath);
            return;
        }

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        New3DSEModWindow.ShowWindow(sourcePath, destinationPath);
    }

    public static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile);
        }

        foreach (string directory in Directory.GetDirectories(sourceDir))
        {
            string destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
            CopyDirectory(directory, destDirectory);
        }
    }

    public static void RenameDirectory(string sourceDir, string destDir)
    {
        if (Directory.Exists(destDir))
        {
            Directory.Delete(destDir, true);
        }

        Directory.Move(sourceDir, destDir);
    }

    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string snakeCase = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower();
        snakeCase = Regex.Replace(snakeCase, @"[\s!@#$%^&*()\-_=+\[\]{};:'"",<.>/?\\|~`]", "_");
        snakeCase = Regex.Replace(snakeCase, @"_+", "_"); // Replace multiple consecutive underscores with a single underscore
        return snakeCase;
    }

    public static void EditManifest(string manifestPath, string authorName, string modName)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(manifestPath);

        XmlNode guidNode = xmlDoc.SelectSingleNode("//guid");
        if (guidNode != null)
        {
            guidNode.InnerText = "com." + authorName + "." + Utils.ToPascalCase(modName);
        }

        XmlNode nameNode = xmlDoc.SelectSingleNode("//name");
        if (nameNode != null)
        {
            nameNode.InnerText = modName;
        }

        XmlNode authorNode = xmlDoc.SelectSingleNode("//author");
        if (authorNode != null)
        {
            authorNode.InnerText = authorName;
        }

        xmlDoc.Save(manifestPath);
    }
}

public class New3DSEModWindow : EditorWindow
{
    private static string sourcePath;
    private static string destinationPath;
    private string userInput = "";
    private string authorName = "";

    public static void ShowWindow(string srcPath, string destPath)
    {
        sourcePath = srcPath;
        destinationPath = destPath;
        GetWindow<New3DSEModWindow>("New 3DSE Mod");
    }

    private void OnGUI()
    {
        GUILayout.Label("Your Mod name:", EditorStyles.boldLabel);
        userInput = EditorGUILayout.TextField("Mod Name", userInput);

        GUILayout.Label("Your Author name:", EditorStyles.boldLabel);
        authorName = EditorGUILayout.TextField("Author Name", authorName);

        if (GUILayout.Button("Create"))
        {
            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(authorName))
            {
                Debug.LogError("Invalid input. Please enter a valid name and author.");
                return;
            }

            string userInputSnakeCase = New3DSEMod.ToSnakeCase(userInput);
            string newDestinationPath = Path.Combine(destinationPath, userInput);
            string newListPath = Path.Combine(newDestinationPath, "List/Studio/" + userInputSnakeCase);

            if (sourcePath == newDestinationPath)
            {
                Debug.LogError("Source and destination path must be different.");
                return;
            }

            try
            {
                New3DSEMod.CopyDirectory(sourcePath, newDestinationPath);
                New3DSEMod.RenameDirectory(Path.Combine(newDestinationPath, "List/Studio/studio_3dse_example"), newListPath);

                // Rename the CSV file
                string oldCsvPath = Path.Combine(newListPath, "ItemGroup_studio_3dse_example.csv");
                string newCsvPath = Path.Combine(newListPath, "ItemGroup_" + userInputSnakeCase + ".csv");
                if (File.Exists(oldCsvPath))
                {
                    File.Move(oldCsvPath, newCsvPath);
                }

                string manifestPath = Path.Combine(newDestinationPath, "manifest.xml");
                if (File.Exists(manifestPath))
                {
                    New3DSEMod.EditManifest(manifestPath, authorName, userInput);
                }

                AssetDatabase.Refresh();
                Debug.Log("New 3DSE mod folder created successfully.");
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during mod folder creation operation: " + ex.Message);
            }
        }
    }
}