using UnityEngine;

public class ProceduralWorld : MonoBehaviour
{
    [Header("Platform Settings")]
    [SerializeField] private GameObject[] platformPrefabs;
    [SerializeField] private int platformsPerLevel = 10;
    
    [Header("Level Configuration")]
    [SerializeField] private float levelHeight = 5f; // Vertical spacing between levels
    [SerializeField] private float randomOffsetRange = 1f; // Random position variation
    
    [Header("Spawn Area")]
    [SerializeField] private Vector3 startPosition = Vector3.zero;
    [SerializeField] private float xMax = 30f; // Maximum X range
    [SerializeField] private float zMax = 30f; // Maximum Z range
    
    void Start()
    {
        GeneratePlatforms();
    }
    
    void GeneratePlatforms()
    {
        if (platformPrefabs == null || platformPrefabs.Length == 0)
        {
            Debug.LogWarning("No platform prefabs assigned!");
            return;
        }
        
        // Calculate grid dimensions
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(platformsPerLevel));
        float cellSizeX = xMax / gridSize;
        float cellSizeZ = zMax / gridSize;
        
        // Spawn platforms on 3 levels
        for (int level = 0; level < 3; level++)
        {
            float currentHeight = startPosition.y + (level * levelHeight);
            int platformCount = 0;
            
            // Create a grid-based distribution
            for (int x = 0; x < gridSize && platformCount < platformsPerLevel; x++)
            {
                for (int z = 0; z < gridSize && platformCount < platformsPerLevel; z++)
                {
                    // Calculate base position in grid cell
                    float basePosX = startPosition.x + (x * cellSizeX) + (cellSizeX * 0.5f);
                    float basePosZ = startPosition.z + (z * cellSizeZ) + (cellSizeZ * 0.5f);
                    
                    // Add random offset within the cell
                    Vector3 spawnPosition = new Vector3(
                        basePosX + Random.Range(-cellSizeX * 0.3f, cellSizeX * 0.3f),
                        currentHeight,
                        basePosZ + Random.Range(-cellSizeZ * 0.3f, cellSizeZ * 0.3f)
                    );
                    
                    // Pick random prefab from the list
                    GameObject prefabToSpawn = platformPrefabs[Random.Range(0, platformPrefabs.Length)];
                    
                    // Spawn the platform
                    GameObject platform = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                    platform.transform.parent = transform; // Organize under this GameObject
                    platform.name = $"Platform_L{level}_P{platformCount}";
                    
                    // Optional: Add random rotation for variety
                    platform.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    
                    platformCount++;
                }
            }
        }
    }
    
    // Call this if you want to regenerate platforms at runtime
    public void RegeneratePlatforms()
    {
        // Clear existing platforms
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        GeneratePlatforms();
    }
}