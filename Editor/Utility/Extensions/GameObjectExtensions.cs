using UnityEngine;

namespace NeonImperium.IconsCreation.Extensions
{
    public static class GameObjectExtensions
    {
        public static Bounds GetOrthographicBounds(this GameObject gameObject, Camera camera)
        {
            if (gameObject == null || camera == null)
                return new Bounds(Vector3.zero, Vector3.zero);

            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            bool hasBounds = false;
            Bounds worldBounds = new Bounds();

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null) continue;
                if (mf.sharedMesh == null) continue;
                if (!mf.gameObject.activeInHierarchy) continue;

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;

                // mesh.bounds — всегда актуальны, не кэшируются как Renderer.bounds
                Bounds local = mf.sharedMesh.bounds;
                Matrix4x4 mat = mf.transform.localToWorldMatrix;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new Vector3(
                        (i & 1) == 0 ? local.min.x : local.max.x,
                        (i & 2) == 0 ? local.min.y : local.max.y,
                        (i & 4) == 0 ? local.min.z : local.max.z);

                    Vector3 world = mat.MultiplyPoint3x4(corner);

                    if (!hasBounds)
                    {
                        worldBounds = new Bounds(world, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        worldBounds.Encapsulate(world);
                    }
                }
            }

            if (!hasBounds)
                return new Bounds(camera.transform.position, Vector3.zero);

            return ProjectBoundsToScreen(worldBounds, camera);
        }

        private static Bounds ProjectBoundsToScreen(Bounds worldBounds, Camera camera)
        {
            Vector3 c = worldBounds.center;
            Vector3 e = worldBounds.extents;

            Vector3 minScreen = Vector3.positiveInfinity;
            Vector3 maxScreen = Vector3.negativeInfinity;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);

                Vector3 sp = camera.WorldToScreenPoint(corner);
                minScreen = Vector3.Min(minScreen, sp);
                maxScreen = Vector3.Max(maxScreen, sp);
            }

            Vector3 min = camera.ScreenToWorldPoint(minScreen);
            Vector3 max = camera.ScreenToWorldPoint(maxScreen);

            Bounds result = new Bounds();
            result.SetMinMax(min, max);
            return result;
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