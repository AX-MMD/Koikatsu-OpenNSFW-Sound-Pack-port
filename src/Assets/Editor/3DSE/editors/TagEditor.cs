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
		string path = Utils.GetLastSelectedPath();
		if (path == null)
		{
			return false;
		}
		else if (Path.GetExtension(path) == TagManager.FileExtention)
		{
			return true;
		}
		else if (Utils.IsValid3DSEModPath(Utils.GetModPath(path)))
		{
			// Check if the file/folder is in Sources folder or subfolder of Sources
			string[] directories = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
			for (int i = 0; i < directories.Length; i++)
			{
				if (directories[i] == "Sources")
				{
					return true;
				}
			}
		}

		return false;
	}

	[MenuItem("Assets/3DSE/Edit 3dse tags", false, 2)]
	public static void CreateTagFiles()
	{
		TagEditorWindow.ShowWindow(Utils.GetLastSelectedPath());
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

		GUILayout.Label("Tags:", EditorStyles.boldLabel);
		validTagsScrollPos = EditorGUILayout.BeginScrollView(validTagsScrollPos, GUILayout.Height(150));
		foreach (string validTag in TagManager.ValidTags)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(validTag);
			if (GUILayout.Button("Add", GUILayout.Width(60)))
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
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Save", GUILayout.Width(position.width / 2)))
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
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Reset", GUILayout.Width(position.width / 2)))
		{
			currentTags = TagManager.LoadTags(selectedPath);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.Width(position.width / 2)))
		{
			this.Close();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
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