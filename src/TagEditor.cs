using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using IllusionMods.Koikatsu3DSEModTools;


public class TagEditor : MonoBehaviour
{
	[MenuItem("Assets/3DSE/Edit 3dse tags", true)]
	private static bool ValidateCreateTagFiles()
	{
		// If is a side panel folder or singular object, check if it is a Sources folder or subfolder of Sources from a 3DSE mod
		if (Selection.activeObject != null)
		{
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			return AssetDatabase.IsValidFolder(path) || Path.GetExtension(path) == TagManager.FileExtention;
		}
		else if (Selection.assetGUIDs.Length == 1 && Selection.objects.Length <= 1)
		{
			string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
			if (AssetDatabase.IsValidFolder(path) && Utils.IsValid3DSEModPath(Utils.GetModPath(path)))
			{
				string[] directories = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
				for (int i = 0; i < directories.Length; i++)
				{
					if (directories[i] == "Sources")
					{
						return true;
					}
				}
			}
		}


		return false;
	}

	[MenuItem("Assets/3DSE/Edit 3dse tags")]
	public static void CreateTagFiles()
	{
		string selectedPath;
		if (Selection.activeObject != null)
		{
			selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
		}
		else
		{
			selectedPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
		}

		TagEditorWindow.ShowWindow(selectedPath);
	}
}


public class TagEditorWindow : EditorWindow
{
	private List<string> currentTags = new List<string>();
	private string selectedPath;
	private Vector2 currentTagsScrollPos;
	private Vector2 validTagsScrollPos;

	public static void ShowWindow(string path)
	{
		TagEditorWindow window = GetWindow<TagEditorWindow>("3DSE/Edit 3dse tags");
		window.selectedPath = path;
		window.currentTags = TagManager.LoadTags(path);
		window.Show();
	}

	private void OnGUI()
	{
		GUILayout.Label("Current Tags:", EditorStyles.boldLabel);

		currentTagsScrollPos = EditorGUILayout.BeginScrollView(currentTagsScrollPos, GUILayout.Height(150));
		foreach (string tag in currentTags)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(tag);
			if (GUILayout.Button("Remove", GUILayout.Width(60)))
			{
				currentTags.Remove(tag);
				break;
			}
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();

		GUILayout.Space(10);

		GUILayout.Label("Add Tags:", EditorStyles.boldLabel);
		validTagsScrollPos = EditorGUILayout.BeginScrollView(validTagsScrollPos, GUILayout.Height(150));
		foreach (string validTag in TagManager.ValidTags)
		{
			if (GUILayout.Button(validTag))
			{
				if (TagManager.ValueTags.Contains(validTag))
				{
					InputDialog.Show("Enter value for " + validTag + ":", (inputValue) =>
						{
							if (!string.IsNullOrEmpty(inputValue))
							{
								currentTags = TagManager.CombineTags(currentTags, new List<string> { validTag + "%%" + inputValue });
							}
						});
				}
				else
				{
					currentTags = TagManager.CombineTags(currentTags, new List<string> { validTag });
				}
			}
		}
		EditorGUILayout.EndScrollView();

		GUILayout.Space(10);

		if (GUILayout.Button("Apply"))
		{
			try
			{
				TagManager.EditTags(selectedPath, currentTags);
				EditorUtility.DisplayDialog("Success", "Tags updated successfully.", "OK");
			}
			catch (TagManager.ValidationError e)
			{
				EditorUtility.DisplayDialog("Error", e.Message, "OK");
			}
		}
		else if (GUILayout.Button("Reset"))
		{
			currentTags = TagManager.LoadTags(selectedPath);
		}
		else if (GUILayout.Button("Close"))
		{
			this.Close();
		}
	}
}

public class InputDialog : EditorWindow
{
	public string inputValue = "";
	private string prompt;
	private System.Action<string> onConfirm;

	public static void Show(string prompt, System.Action<string> onConfirm)
	{
		InputDialog window = ScriptableObject.CreateInstance<InputDialog>();
		window.prompt = prompt;
		window.onConfirm = onConfirm;
		window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 100);
		window.ShowUtility();
	}

	private void OnGUI()
	{
		GUILayout.Label(prompt, EditorStyles.wordWrappedLabel);
		inputValue = EditorGUILayout.TextField(inputValue);

		GUILayout.Space(10);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("OK"))
		{
			onConfirm(inputValue);
			this.Close();
		}
		if (GUILayout.Button("Cancel"))
		{
			this.Close();
		}
		EditorGUILayout.EndHorizontal();
	}
}