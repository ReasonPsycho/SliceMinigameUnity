
using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float playerHeightOffset = 1f; // Height above platform to place player
    
    private List<Transform> allPlatforms = new List<Transform>();
    
    void Start()
    {
        GeneratePlatforms();
        PlacePlayerOnHighestPlatform();
    }
    
    void GeneratePlatforms()
    {
        if (platformPrefabs == null || platformPrefabs.Length == 0)
        {
            Debug.LogWarning("No platform prefabs assigned!");
            return;
        }
        
        allPlatforms.Clear();
        
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
                    
                    allPlatforms.Add(platform.transform);
                    
                    platformCount++;
                }
            }
        }
    }
    
    void PlacePlayerOnHighestPlatform()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform not assigned!");
            return;
        }
        
        if (allPlatforms.Count == 0)
        {
            Debug.LogWarning("No platforms available to place player!");
            return;
        }
        
        // Find the highest level (level 2)
        float highestY = float.MinValue;
        foreach (Transform platform in allPlatforms)
        {
            if (platform.position.y > highestY)
            {
                highestY = platform.position.y;
            }
        }
        
        // Find platforms on the highest level
        List<Transform> highestPlatforms = new List<Transform>();
        foreach (Transform platform in allPlatforms)
        {
            if (Mathf.Approximately(platform.position.y, highestY))
            {
                highestPlatforms.Add(platform);
            }
        }
        
        // Find the closest platform to start position
        Transform closestPlatform = null;
        float closestDistance = float.MaxValue;
        
        foreach (Transform platform in highestPlatforms)
        {
            float distance = Vector3.Distance(new Vector3(platform.position.x, 0, platform.position.z), 
                                              new Vector3(startPosition.x, 0, startPosition.z));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlatform = platform;
            }
        }
        
        // Place player on the closest highest platform
        if (closestPlatform != null)
        {
            Vector3 playerPosition = closestPlatform.position;
            playerPosition.y += playerHeightOffset;
            playerTransform.position = playerPosition;
            
            Debug.Log($"Player placed on platform: {closestPlatform.name} at position: {playerPosition}");
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
        PlacePlayerOnHighestPlatform();
    }
}