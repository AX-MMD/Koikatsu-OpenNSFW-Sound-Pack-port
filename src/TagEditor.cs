using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using IllusionMods.Koikatsu3DSEModTools;

public class TagEditor : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Edit 3dse tags", true)]
    private static bool ValidateCreateTagFiles()
    {
        return Selection.activeObject != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Assets/3DSE/Edit 3dse tags")]
    public static void CreateTagFiles()
    {
        string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        TagEditorWindow.ShowWindow(selectedPath);
    }
}

public class TagEditorWindow : EditorWindow
{
    private string tagsInput = "";
    private string selectedPath;

    public static void ShowWindow(string path)
    {
        TagEditorWindow window = GetWindow<TagEditorWindow>("3DSE/Edit 3dse tags");
        window.selectedPath = path;
        window.tagsInput = "[" + string.Join("][", TagManager.GetTags(path).ToArray()) + "]";
        window.Show();
    }

    private void OnGUI()
    {

        GUILayout.Label("Enter tags e.g. [tag1][tag2]:", EditorStyles.boldLabel);
        tagsInput = EditorGUILayout.TextField("Tags", tagsInput);

        if (GUILayout.Button("Apply"))
        {
            try
            {
                TagManager.EditTags(selectedPath, tagsInput);
                EditorUtility.DisplayDialog("Success", "Tags generation completed.", "OK");
                this.Close();
            }
            catch (TagManager.ValidationError e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }
    }
}