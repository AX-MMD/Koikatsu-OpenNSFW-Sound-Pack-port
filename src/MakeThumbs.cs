using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using IllusionMods.KoikatsuStudioCsv;
using IllusionMods.Koikatsu3DSEModTools;

public class MakeThumbs : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Tools/Make Thumbnails")]
    public static void MakeThumbnails()
    {
        IEnumerable<string> selectedPaths = Utils.GetSelected3DSEModPaths();
        foreach (string selectedPath in selectedPaths)
        {
            try
            {
                CsvUtils.ItemFileAggregate itemFileAggregate = CsvUtils.GetItemFileAggregate(selectedPath);
                List<CsvUtils.StudioItem> entries = itemFileAggregate.GetEntries<CsvUtils.StudioItem>();

                string baseThumbPath = Path.Combine(selectedPath, "Studio_Thumbs/base.png");
                if (!File.Exists(baseThumbPath))
                {
                    Debug.LogError("Base thumbnail not found at path: " + baseThumbPath);
                    continue;
                }

                string thumbsOutputPath = Path.Combine(selectedPath, "Studio_Thumbs");
                if (!Directory.Exists(thumbsOutputPath))
                {
                    Directory.CreateDirectory(thumbsOutputPath);
                }

                foreach (CsvUtils.StudioItem entry in entries)
                {
                    string newThumbPath = Path.Combine(thumbsOutputPath, string.Format("{0}-{1}-{2}.png", entry.groupNumber.PadLeft(8, '0'), entry.categoryNumber.PadLeft(8, '0'), entry.name));
                    File.Copy(baseThumbPath, newThumbPath, true);
                }

                Debug.Log("Thumbnails created successfully for path: " + selectedPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error creating thumbnails for path: " + selectedPath + "\n" + e.Message);
            }
        }

        AssetDatabase.Refresh();
    }
}