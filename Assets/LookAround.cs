
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LookAround : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private UniversalRendererData rendererData;
    
    private Camera mainCamera;
    private GlobalSliceRenderFeature sliceRenderFeature;

    void Start()
    {
        // Get reference to the main camera
        mainCamera = Camera.main;
        
        // Find the GlobalSliceRenderFeature in the renderer data
        if (rendererData != null)
        {
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is GlobalSliceRenderFeature)
                {
                    sliceRenderFeature = feature as GlobalSliceRenderFeature;
                    break;
                }
            }
        }
        
        // Ensure the trigger collider is set properly
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Ensure the target camera is disabled at start
        if (targetCamera != null)
        {
            targetCamera.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Switch to the target camera
            if (targetCamera != null && mainCamera != null)
            {
                mainCamera.enabled = false;
                targetCamera.enabled = true;
            }
            
            // Disable the GlobalSliceRenderFeature
            if (sliceRenderFeature != null)
            {
                sliceRenderFeature.SetActive(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Switch back to the main camera
            if (targetCamera != null && mainCamera != null)
            {
                targetCamera.enabled = false;
                mainCamera.enabled = true;
            }
            
            // Enable the GlobalSliceRenderFeature
            if (sliceRenderFeature != null)
            {
                sliceRenderFeature.SetActive(true);
            }
        }
    }
}