using UnityEngine;
using System.Collections.Generic;

public class GlobalSliceController : MonoBehaviour
{
    [Header("Slice Settings")]
    [SerializeField] private Transform slicePlaneTransform;
    [SerializeField] private float threshold = 0.02f;
    [SerializeField] private float colorThreshold = 0.02f;
    [SerializeField] private bool enableSlicing = true;
    [SerializeField] private Color sliceColor = Color.red; // Add this line
    
    private static readonly int SlicePlaneID = Shader.PropertyToID("_SlicePlane");
    private static readonly int ThresholdID = Shader.PropertyToID("_Threshold");
    private static readonly int SliceColorID = Shader.PropertyToID("_SliceColor"); // Add this line
    private static readonly int ColorThreshold = Shader.PropertyToID("_ColorThreshold"); // Add this line
    
    private void Update()
    {
        UpdateSlicePlane();
    }
    
    private void UpdateSlicePlane()
    {
        if (slicePlaneTransform == null)
            return;
        
        // Plane equation: ax + by + cz = d
        Vector3 normal = slicePlaneTransform.up;
        Vector3 position = slicePlaneTransform.position;
        float d = Vector3.Dot(normal, position);
        
        Vector4 plane = new Vector4(normal.x, normal.y, normal.z, d);
        
        // Set global shader properties (works for shaders that use these properties)
        Shader.SetGlobalVector(SlicePlaneID, plane);
        Shader.SetGlobalFloat(ThresholdID, threshold);
        Shader.SetGlobalFloat(ColorThreshold, colorThreshold);
        Shader.SetGlobalColor(SliceColorID, sliceColor); // Add this line
        
    }
    
    private void OnDrawGizmos()
    {
        if (slicePlaneTransform == null)
            return;
        
        // Visualize the slice plane
        Gizmos.color = Color.yellow;
        Gizmos.matrix = slicePlaneTransform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(5f, 0.01f, 5f));
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(Vector3.zero, Vector3.up * 2f);
    }
}