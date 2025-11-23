using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MeshSliceManipulator : MonoBehaviour
{
    [SerializeField] private Transform slicePlaneTransform;
    [SerializeField] private Camera orthographicCamera;
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private float selectionRadius = 0.5f;
    [SerializeField] private float vertexInfluenceRadius = 1f;

    private List<Vector3> intersectionPoints = new List<Vector3>();
    private List<Vector3> interactivePoints = new List<Vector3>(); // New list for created points
    private Dictionary<MeshFilter, List<int>> newVertexIndices = new Dictionary<MeshFilter, List<int>>(); // Track new vertices per mesh
    private List<MeshVertexData> affectedVertices = new List<MeshVertexData>();
    private Vector3? dragStartPoint;
    private Vector3? selectedPoint;
    private bool isDragging;
    private Vector3 currentMouseWorldPosition;

    private class MeshVertexData
    {
        public MeshFilter MeshFilter;
        public int VertexIndex;
        public Vector3 OriginalPosition;
        public float DistanceToSelected;
        public bool IsNewVertex; // Track if this is a newly created vertex
    }
    
    private void Update()
    {
        if (slicePlaneTransform == null) return;
        currentMouseWorldPosition = GetWorldPositionAtMouse();
        HandleInput();
    }

    private Vector3 GetWorldPositionAtMouse()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 screenPoint = new Vector3(mousePosition.x, mousePosition.y, orthographicCamera.nearClipPlane);
        Vector3 worldPoint = orthographicCamera.ScreenToWorldPoint(screenPoint);
        worldPoint.y = slicePlaneTransform.position.y;
        return worldPoint;
    }

   
    private void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // First try to select an existing point (either intersection or interactive)
            if (TrySelectNearestPoint(currentMouseWorldPosition, out Vector3 nearestPoint))
            {
                selectedPoint = nearestPoint;
                dragStartPoint = currentMouseWorldPosition;
                isDragging = true;
                CollectAffectedVertices();
            }
            // If no point was found, try to create a new point on a line
            else
            {
                Vector3? newPoint = TryCreatePointOnNearestLine(currentMouseWorldPosition);
                if (newPoint.HasValue)
                {
                    interactivePoints.Add(newPoint.Value);
                    selectedPoint = newPoint.Value;
                    dragStartPoint = currentMouseWorldPosition;
                    isDragging = true;
                    
                    // Create actual vertices in the mesh at this point
                    CreateVerticesAtPoint(newPoint.Value);
                    CollectAffectedVertices();
                }
            }
        }
        else if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector3 dragDelta = currentMouseWorldPosition - dragStartPoint.Value;
            Vector3 planeNormal = slicePlaneTransform.up;
            Vector3 projectedDelta = Vector3.ProjectOnPlane(dragDelta, planeNormal);
            UpdateMeshVertices(projectedDelta);

            // Update the interactive point position if we're dragging one
            if (selectedPoint.HasValue)
            {
                int index = interactivePoints.IndexOf(selectedPoint.Value);
                if (index != -1)
                {
                    interactivePoints[index] = selectedPoint.Value + projectedDelta;
                    selectedPoint = interactivePoints[index];
                }
            }
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            selectedPoint = null;
            dragStartPoint = null;
            affectedVertices.Clear();
        }
    }

    private bool TrySelectNearestPoint(Vector3 clickPoint, out Vector3 nearestPoint)
    {
        float minDistance = selectionRadius;
        nearestPoint = Vector3.zero;
        bool found = false;

        // Check intersection points
        foreach (Vector3 point in intersectionPoints)
        {
            float distance = Vector2.Distance(
                new Vector2(clickPoint.x, clickPoint.z),
                new Vector2(point.x, point.z)
            );
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = point;
                found = true;
            }
        }

        // Check interactive points
        foreach (Vector3 point in interactivePoints)
        {
            float distance = Vector2.Distance(
                new Vector2(clickPoint.x, clickPoint.z),
                new Vector2(point.x, point.z)
            );
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = point;
                found = true;
            }
        }

        return found;
    }

    private Vector3? TryCreatePointOnNearestLine(Vector3 clickPoint)
    {
        float minDistance = selectionRadius;
        Vector3? newPoint = null;

        // Check each pair of intersection points
        for (int i = 0; i < intersectionPoints.Count - 1; i += 2)
        {
            Vector3 lineStart = intersectionPoints[i];
            Vector3 lineEnd = intersectionPoints[i + 1];

            Vector2 start2D = new Vector2(lineStart.x, lineStart.z);
            Vector2 end2D = new Vector2(lineEnd.x, lineEnd.z);
            Vector2 click2D = new Vector2(clickPoint.x, clickPoint.z);

            // Project point onto line segment
            Vector2 line = end2D - start2D;
            float length = line.magnitude;
            Vector2 lineDir = line / length;

            Vector2 v = click2D - start2D;
            float t = Vector2.Dot(v, lineDir);

            // Check if projection is within line segment
            if (t >= 0 && t <= length)
            {
                Vector2 projection = start2D + lineDir * t;
                float distance = Vector2.Distance(click2D, projection);

                if (distance < minDistance)
                {
                    float ratio = t / length;
                    newPoint = Vector3.Lerp(lineStart, lineEnd, ratio);
                    minDistance = distance;
                }
            }
        }

        return newPoint;
    }

    private void CollectAffectedVertices()
    {
        affectedVertices.Clear();
        if (!selectedPoint.HasValue) return;

        float sliceHeight = slicePlaneTransform.position.y;
        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        
        foreach (MeshFilter meshFilter in meshFilters)
        {
            // Ignore objects with "Player" tag
            if (meshFilter.CompareTag("Player"))
                continue;
            
            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = meshFilter.transform.TransformPoint(vertices[i]);
                
                // Project vertex onto slice plane
                Vector3 toVertex = worldPos - slicePlaneTransform.position;
                float distanceToSlice = Mathf.Abs(Vector3.Dot(toVertex, slicePlaneTransform.up));
                
                // Project points onto slice plane for distance calculation
                Vector3 projectedVertex = Vector3.ProjectOnPlane(toVertex, slicePlaneTransform.up) + 
                                        slicePlaneTransform.position;
                float distanceToSelected = Vector3.Distance(projectedVertex, selectedPoint.Value);

                if (distanceToSlice < vertexInfluenceRadius && distanceToSelected < vertexInfluenceRadius)
                {
                    affectedVertices.Add(new MeshVertexData
                    {
                        MeshFilter = meshFilter,
                        VertexIndex = i,
                        OriginalPosition = vertices[i],
                        DistanceToSelected = distanceToSelected
                    });
                }
            }
        }
    }

    private void UpdateMeshVertices(Vector3 dragDelta)
    {
        Debug.Log("Updating mesh vertices");
        Dictionary<MeshFilter, bool> modifiedMeshes = new Dictionary<MeshFilter, bool>();

        foreach (var vertexData in affectedVertices)
        {
            MeshFilter meshFilter = vertexData.MeshFilter;
            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;

            float influence = 1 - (vertexData.DistanceToSelected / vertexInfluenceRadius);
            influence = Mathf.Clamp01(influence);

            Vector3 localDragDelta = meshFilter.transform.InverseTransformDirection(dragDelta);
            vertices[vertexData.VertexIndex] = vertexData.OriginalPosition + localDragDelta * influence;

            mesh.vertices = vertices;
            modifiedMeshes[meshFilter] = true;
        }

        foreach (var kvp in modifiedMeshes)
        {
            MeshFilter meshFilter = kvp.Key;
            Mesh mesh = meshFilter.mesh;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = mesh;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (slicePlaneTransform == null) return;
        
        intersectionPoints.Clear();
        
        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        
        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) continue;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            
            Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[triangles[i]]);
                Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                Vector3 v3 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 2]]);

                FindIntersectionPoints(v1, v2, v3);
            }
        }

        // Draw intersection lines
        Gizmos.color = lineColor;
        for (int i = 0; i < intersectionPoints.Count - 1; i += 2)
        {
            Gizmos.DrawLine(intersectionPoints[i], intersectionPoints[i + 1]);
        }

        // Draw interactive points
        Gizmos.color = Color.green;
        foreach (Vector3 point in interactivePoints)
        {
            Gizmos.DrawWireSphere(point, 0.15f);
            Gizmos.DrawSphere(point, 0.1f);
        }

        // Draw mouse position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(currentMouseWorldPosition, 0.1f);
            Gizmos.color = new Color(0, 0, 1, 0.2f);
            Gizmos.DrawSphere(currentMouseWorldPosition, 0.1f);
            
            // Draw selection radius
            Gizmos.color = new Color(0, 0, 1, 0.1f);
            DrawCircleOnPlane(currentMouseWorldPosition, selectionRadius);
        }

        // Draw selected point
        if (selectedPoint.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(selectedPoint.Value, 0.1f);
            
            if (isDragging)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                DrawCircleOnPlane(selectedPoint.Value, vertexInfluenceRadius);
            }
        }

        // Draw slice plane reference
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Matrix4x4 sliceMatrix = Matrix4x4.TRS(
            slicePlaneTransform.position,
            slicePlaneTransform.rotation,
            new Vector3(10f, 0.01f, 10f)
        );
        Gizmos.matrix = sliceMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawCircleOnPlane(Vector3 center, float radius)
    {
        int segments = 32;
        Vector3 prevPoint = Vector3.zero;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            Vector3 right = Vector3.Cross(slicePlaneTransform.up, Vector3.forward).normalized;
            Vector3 forward = Vector3.Cross(right, slicePlaneTransform.up);
            Vector3 point = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;
            
            if (i > 0)
                Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }


    private void FindIntersectionPoints(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        if (slicePlaneTransform == null) return;

        float sliceHeight = slicePlaneTransform.position.y;
        Vector3 planeNormal = slicePlaneTransform.up;
        
        // Convert points to plane space
        float d1 = Vector3.Dot(v1 - slicePlaneTransform.position, planeNormal);
        float d2 = Vector3.Dot(v2 - slicePlaneTransform.position, planeNormal);
        float d3 = Vector3.Dot(v3 - slicePlaneTransform.position, planeNormal);

        bool above1 = d1 > 0;
        bool above2 = d2 > 0;
        bool above3 = d3 > 0;

        if ((above1 && above2 && above3) || (!above1 && !above2 && !above3))
            return;

        if (above1 != above2)
            intersectionPoints.Add(GetIntersectionPoint(v1, v2, d1, d2));
        if (above2 != above3)
            intersectionPoints.Add(GetIntersectionPoint(v2, v3, d2, d3));
        if (above3 != above1)
            intersectionPoints.Add(GetIntersectionPoint(v3, v1, d3, d1));
    }

    private Vector3 GetIntersectionPoint(Vector3 p1, Vector3 p2, float d1, float d2)
    {
        float t = d1 / (d1 - d2);
        return Vector3.Lerp(p1, p2, t);
    }
    
    private void CreateVerticesAtPoint(Vector3 worldPoint)
    {
        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        
        foreach (MeshFilter meshFilter in meshFilters)
        {
            // Ignore objects with "Player" tag
            if (meshFilter.CompareTag("Player"))
                continue;
            
            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            
            List<Vector3> newVertices = new List<Vector3>(vertices);
            List<int> newTriangles = new List<int>(triangles);
            List<Vector3> newNormals = new List<Vector3>(normals);
            List<Vector2> newUVs = new List<Vector2>(uvs);
            
            if (!newVertexIndices.ContainsKey(meshFilter))
                newVertexIndices[meshFilter] = new List<int>();
            
            // Find triangles that are intersected by the slice plane near this point
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = meshFilter.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v2 = meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v3 = meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]);
                
                // Check if this triangle is intersected by the slice plane
                List<Vector3> intersections = new List<Vector3>();
                if (CheckEdgeIntersection(v1, v2, out Vector3 intersection1))
                    intersections.Add(intersection1);
                if (CheckEdgeIntersection(v2, v3, out Vector3 intersection2))
                    intersections.Add(intersection2);
                if (CheckEdgeIntersection(v3, v1, out Vector3 intersection3))
                    intersections.Add(intersection3);
                
                // If we have 2 intersection points and our worldPoint is near this line
                if (intersections.Count == 2)
                {
                    Vector3 midPoint = (intersections[0] + intersections[1]) * 0.5f;
                    float distanceToLine = Vector3.Distance(worldPoint, midPoint);
                    
                    if (distanceToLine < vertexInfluenceRadius)
                    {
                        // Create a new vertex at the world point
                        Vector3 localPoint = meshFilter.transform.InverseTransformPoint(worldPoint);
                        int newVertexIndex = newVertices.Count;
                        newVertices.Add(localPoint);
                        
                        // Interpolate normal and UV
                        Vector3 barycentricCoord = GetBarycentricCoordinate(
                            vertices[triangles[i]], 
                            vertices[triangles[i + 1]], 
                            vertices[triangles[i + 2]], 
                            localPoint);
                        
                        Vector3 interpolatedNormal = 
                            normals[triangles[i]] * barycentricCoord.x +
                            normals[triangles[i + 1]] * barycentricCoord.y +
                            normals[triangles[i + 2]] * barycentricCoord.z;
                        newNormals.Add(interpolatedNormal.normalized);
                        
                        if (uvs.Length > 0)
                        {
                            Vector2 interpolatedUV = 
                                uvs[triangles[i]] * barycentricCoord.x +
                                uvs[triangles[i + 1]] * barycentricCoord.y +
                                uvs[triangles[i + 2]] * barycentricCoord.z;
                            newUVs.Add(interpolatedUV);
                        }
                        
                        // Split the triangle into 3 smaller triangles using the new vertex
                        int idx0 = triangles[i];
                        int idx1 = triangles[i + 1];
                        int idx2 = triangles[i + 2];
                        
                        // Remove old triangle
                        newTriangles.RemoveRange(i, 3);
                        
                        // Add three new triangles
                        newTriangles.AddRange(new int[] { idx0, idx1, newVertexIndex });
                        newTriangles.AddRange(new int[] { idx1, idx2, newVertexIndex });
                        newTriangles.AddRange(new int[] { idx2, idx0, newVertexIndex });
                        
                        // Track this new vertex
                        newVertexIndices[meshFilter].Add(newVertexIndex);
                    }
                }
            }
            
            // Apply the modified mesh
            mesh.Clear();
            mesh.vertices = newVertices.ToArray();
            mesh.triangles = newTriangles.ToArray();
            mesh.normals = newNormals.ToArray();
            if (newUVs.Count > 0)
                mesh.uv = newUVs.ToArray();
            
            mesh.RecalculateBounds();
            
            // Update collider if present
            MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = mesh;
            }
        }
    }
    
    private bool CheckEdgeIntersection(Vector3 v1, Vector3 v2, out Vector3 intersection)
    {
        Vector3 planeNormal = slicePlaneTransform.up;
        float d1 = Vector3.Dot(v1 - slicePlaneTransform.position, planeNormal);
        float d2 = Vector3.Dot(v2 - slicePlaneTransform.position, planeNormal);
        
        // Check if edge crosses plane
        if ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
        {
            float t = d1 / (d1 - d2);
            intersection = Vector3.Lerp(v1, v2, t);
            return true;
        }
        
        intersection = Vector3.zero;
        return false;
    }
    
    private Vector3 GetBarycentricCoordinate(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p)
    {
        Vector3 v0 = v2 - v1;
        Vector3 v1v = v3 - v1;
        Vector3 v2v = p - v1;
        
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1v);
        float d11 = Vector3.Dot(v1v, v1v);
        float d20 = Vector3.Dot(v2v, v0);
        float d21 = Vector3.Dot(v2v, v1v);
        
        float denom = d00 * d11 - d01 * d01;
        if (Mathf.Abs(denom) < 0.0001f)
            return new Vector3(1f / 3f, 1f / 3f, 1f / 3f);
        
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;
        
        return new Vector3(u, v, w);
    }
}