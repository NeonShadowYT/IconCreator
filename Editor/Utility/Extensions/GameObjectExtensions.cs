using UnityEngine;

namespace NeonImperium.IconsCreation.Extensions
{
    public static class GameObjectExtensions
    {
        public static Bounds GetOrthographicBounds(this GameObject gameObject, Camera camera)
        {
            if (gameObject == null || camera == null)
                return new Bounds(Vector3.zero, Vector3.zero);

            Vector3 minScreenPosition = Vector3.positiveInfinity;
            Vector3 maxScreenPosition = Vector3.negativeInfinity;

            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (!meshFilter || !meshFilter.sharedMesh)
                    continue;
                
                Vector3[] vertices = meshFilter.sharedMesh.vertices;

                foreach (Vector3 vertex in vertices)
                {
                    Vector3 wsVertexPosition = meshFilter.transform.TransformPoint(vertex);
                    Vector3 screenPosition = camera.WorldToScreenPoint(wsVertexPosition);

                    minScreenPosition.x = Mathf.Min(minScreenPosition.x, screenPosition.x);
                    minScreenPosition.y = Mathf.Min(minScreenPosition.y, screenPosition.y);
                    minScreenPosition.z = Mathf.Min(minScreenPosition.z, screenPosition.z);
                    
                    maxScreenPosition.x = Mathf.Max(maxScreenPosition.x, screenPosition.x);
                    maxScreenPosition.y = Mathf.Max(maxScreenPosition.y, screenPosition.y);
                    maxScreenPosition.z = Mathf.Max(maxScreenPosition.z, screenPosition.z);
                }
            }
            
            if (minScreenPosition.x == float.PositiveInfinity)
                return new Bounds(camera.transform.position, Vector3.zero);

            Vector3 min = camera.ScreenToWorldPoint(minScreenPosition);
            Vector3 max = camera.ScreenToWorldPoint(maxScreenPosition);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            
            return bounds;
        }

        public static bool HasVisibleMesh(this GameObject gameObject)
        {
            if (gameObject == null) return false;

            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (!meshFilter) continue;
                if (!meshFilter.sharedMesh) continue;
                if (!meshFilter.GetComponent<MeshRenderer>()) continue;
                
                return true;
            }

            return false;
        }
    }
}