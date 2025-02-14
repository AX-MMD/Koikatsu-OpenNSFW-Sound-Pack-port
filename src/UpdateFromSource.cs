using UnityEngine;
using UnityEditor;

public class UpdateFromSource : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Update From Source")]
    public static void RegenAll()
    {
        Debug.Log("Update From Source started...");
        // Execute GeneratePrefabsFromWav
        GeneratePrefabsFromSource.GeneratePrefabs();

        // Execute GenerateItemListFromPrefabs
        GenerateItemListFromPrefabs.GenerateCSV();

        Debug.Log("Update From Source completed successfully.");
    }
}