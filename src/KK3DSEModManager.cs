using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Studio.Sound;
using System.Text;
using System.Linq;
using ActionGame.MapSound;
using IllusionMods.KoikatsuStudioCsv;
using IllusionMods.Koikatsu3DSECategoryTool;

namespace IllusionMods.Koikatsu3DSEModTools {

	public class KK3DSEModManager
	{
		public const string BackupPath = "3DSECsvBackup";

		public string modPath;
		public string modName;
		public string csvFolderPath;
		public string prefabOutputPath;
		public string sourcesPath;
		public string basePrefabPath;
		public UnityEngine.GameObject basePrefab;

		private bool disposed = false;


		private static void AssertIsModPath(string modPath)
		{
			if (Directory.GetParent(modPath).Name != "Mods")
			{
				throw new Exception("Invalid mod path: " + modPath);
			}
		}

		public static string GetSourceFolderPath(string path)
		{
			AssertIsModPath(path);
			return Path.Combine(path, "Sources");
		}

		public static string GetPrefabFolderPath(string path)
		{
			AssertIsModPath(path);
			return Path.Combine(path, "Prefab");
		}

		public static string GetBasePrefabPath(string path)
		{
			AssertIsModPath(path);
			return Path.Combine(path, "base_3dse.prefab");
		}

		public KK3DSEModManager(string selectedPath)
		{
			this.modPath = Utils.GetModPath(selectedPath, true);
			this.modName = Utils.GetModName(this.modPath);
			this.csvFolderPath = CsvUtils.GetItemDataFolder(this.modPath);
			this.prefabOutputPath = GetPrefabFolderPath(selectedPath);
			if (!Directory.Exists(prefabOutputPath))
			{
				Directory.CreateDirectory(prefabOutputPath);
				Debug.Log("Created output directory: " + prefabOutputPath);
			}
			this.sourcesPath = GetSourceFolderPath(selectedPath);
			if (!Directory.Exists(sourcesPath))
			{
				throw new Exception("Sources folder not found at path: " + sourcesPath);
			}
			this.basePrefabPath = GetBasePrefabPath(selectedPath);
			if (!File.Exists(this.basePrefabPath))
			{
				throw new Exception("Base prefab not found at path: " + this.basePrefabPath);
			}
			this.basePrefab = (GameObject)AssetDatabase.LoadAssetAtPath(this.basePrefabPath, typeof(GameObject));
			if (this.basePrefab == null)
			{
				throw new Exception("Base prefab not found at path: " + this.basePrefabPath);
			}

			this.BackupCSV();
		}

		public Utils.GenerationResult GenerateCSV(bool create, IList<Category> categories)
		{
			return CsvUtils.GenerateCSV(this.modPath, create, categories);
		}

		public Utils.GenerationResult GeneratePrefabs(bool create, IList<Category> categories)
		{
			if (!Directory.Exists(this.prefabOutputPath))
			{
				Directory.CreateDirectory(this.prefabOutputPath);
				Debug.Log("Created output directory: " + this.prefabOutputPath);
			}
			else if (create)
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

			HashSet<string> oldPrefabs = new HashSet<string>();
			string[] oldPrefabPaths = Directory.GetFiles(this.prefabOutputPath, "*.prefab", SearchOption.AllDirectories);
			foreach (string oldPrefabPath in oldPrefabPaths)
			{
				oldPrefabs.Add(oldPrefabPath.Replace("\\", "/"));
			}

			int prefabCreateCount = 0;
			int prefabUpdateCount = 0;
			int categoryCount = 0;
			int totalCategories = categories.Where(x => x.items.Count > 0).Count();
			string progressName = create ? "Generating " + this.modName : "Updating " + this.modName;

			foreach (Category category in categories.Where(x => x.items.Count > 0))
			{	
				string categoryOutputPath = Path.Combine(this.prefabOutputPath, category.GetKey()).Replace("\\", "/");
				string assetBundlePath = Utils.GetAssetBundlePath(this.modPath, category.GetKey());

				if (!Directory.Exists(categoryOutputPath))
				{
					Directory.CreateDirectory(categoryOutputPath);
					Debug.Log("Created output directory: " + categoryOutputPath);
				}
				else if (create)
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

				int itemCount = 0;
				categoryCount++;
				foreach (StudioItemParam file in category.items)
				{
					itemCount++;
					EditorUtility.DisplayProgressBar(
						progressName, 
						string.Format("Category ({0}/{1}): {2} -> ({3}/{4})", categoryCount, totalCategories, category.GetKey(), itemCount, category.items.Count),
						(float)itemCount / category.items.Count
					);

					string newPrefabPath = Path.Combine(categoryOutputPath, file.prefabName + ".prefab").Replace("\\", "/");
					GameObject prefab;

					if (oldPrefabs.Contains(newPrefabPath))
					{
						oldPrefabs.Remove(newPrefabPath);
						prefab = (GameObject)PrefabUtility.InstantiatePrefab(
							AssetDatabase.LoadAssetAtPath(newPrefabPath, typeof(GameObject))
						);
						prefabUpdateCount++;
					}
					else
					{
						prefab = (GameObject)PrefabUtility.InstantiatePrefab(this.basePrefab);
						prefabCreateCount++;
					}

					// Load the AudioClip from the .wav file
					AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(file.path);
					if (audioClip == null)
					{
						throw new Exception("AudioClip not found at path: " + file.path);
					}

					// Assign the AudioClip to the SEComponent
					SEComponent seComponent = prefab.GetComponent<SEComponent>();
					EditorUtility.SetDirty(seComponent);
					if (seComponent != null)
					{
						seComponent._clip = audioClip;
						if (file.prefabModifier.isLoop)
						{
							seComponent._isLoop = true;
						}
						if (file.prefabModifier.threshold != null)
						{
							seComponent._rolloffDistance = new Threshold(file.prefabModifier.threshold.Item1, file.prefabModifier.threshold.Item2);
						}
						if (file.prefabModifier.volume != -1.0f)
						{
							seComponent._volume = file.prefabModifier.volume;
						}
					}
					else
					{
						throw new Exception("SEComponent not found on the instantiated prefab.");
					}

					PrefabUtility.CreatePrefab(newPrefabPath, prefab);
					UnityEngine.Object.DestroyImmediate(prefab);

					// Set the asset bundle name in the .meta file
					string metaFilePath = newPrefabPath + ".meta";
					if (File.Exists(metaFilePath))
					{
						Utils.SetAssetBundleNameInMetaFile(metaFilePath, assetBundlePath);
					}
					else
					{
						throw new Exception("Meta file not found for prefab: " + newPrefabPath);
					}
				}
			}
			// Delete any old prefabs that were not updated
			foreach (string oldPrefab in oldPrefabs)
			{
				File.Delete(oldPrefab);
				File.Delete(oldPrefab + ".meta");
			}

			// Delete empty directories
			foreach (string categoryOutputPath in Directory.GetDirectories(this.prefabOutputPath))
			{
				if (Directory.GetFiles(categoryOutputPath).Length == 0)
				{
					Directory.Delete(categoryOutputPath);
					File.Delete(categoryOutputPath + ".meta");
				}
			}
			
			return new Utils.GenerationResult
			{
				createCount = prefabCreateCount,
				updateCount = prefabUpdateCount,
				deleteCount = oldPrefabs.Count
			};
		}

		public void BackupCSV()
		{
			string backupPath = Path.Combine(this.csvFolderPath, BackupPath);
			if (!Directory.Exists(backupPath))
			{
				Directory.CreateDirectory(backupPath);
			}
			else
			{
				foreach (string file in Directory.GetFiles(backupPath, "*.csv"))
				{
					File.Delete(file);
					File.Delete(file + ".meta");
				}
			}

			foreach (string file in Directory.GetFiles(this.csvFolderPath, "*.csv"))
			{
				File.Copy(file, Path.Combine(backupPath, Path.GetFileName(file)), true);
			}
		}

		public void RestoreBackupCSV()
		{
			string backupPath = Path.Combine(this.csvFolderPath, BackupPath);
			if (Directory.Exists(backupPath))
			{
				foreach (string file in Directory.GetFiles(this.csvFolderPath, "*.csv"))
				{
					File.Delete(file);
					File.Delete(file + ".meta");
				}

				foreach (string file in Directory.GetFiles(backupPath, "*.csv"))
				{
					File.Copy(file, Path.Combine(this.csvFolderPath, Path.GetFileName(file)), true);
				}
			}
		}

		public void DeleteBackupCSV()
		{
			string backupPath = Path.Combine(this.csvFolderPath, BackupPath);
			if (Directory.Exists(backupPath))
			{
				foreach (string file in Directory.GetFiles(backupPath, "*.csv"))
				{
					File.Delete(file);
					File.Delete(file + ".meta");
				}
				Directory.Delete(backupPath);
				File.Delete(backupPath + ".meta");
			}
		}

		public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.DeleteBackupCSV();
                }

                disposed = true;
            }
        }

        ~KK3DSEModManager()
        {
            Dispose(false);
        }
	}
}