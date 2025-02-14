using UnityEngine;
using UnityEditor;

public class GenerateFromSource : MonoBehaviour
{
    [MenuItem("Assets/3DSE/Generate From Source")]
    public static void RegenAll()
    {
        Debug.Log("Generate From Source started...");
        // Execute GeneratePrefabsFromWav
        GeneratePrefabsFromSource.GeneratePrefabs(true);

        // Execute GenerateItemListFromPrefabs
        GenerateItemListFromPrefabs.GenerateCSV(true);

        Debug.Log("Generate From Source completed successfully.");
    }
}