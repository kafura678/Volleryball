using UnityEngine;

namespace Volleyball
{
    static class MarkerVisualFactory
    {
        const int DiscSegments = 32;

        public static GameObject CreateDisc(string name)
        {
            var gameObject = CreateVisual(name);
            gameObject.GetComponent<MeshFilter>().sharedMesh = CreateDiscMesh();
            return gameObject;
        }

        public static GameObject CreateBox(string name)
        {
            var gameObject = CreateVisual(name);
            gameObject.GetComponent<MeshFilter>().sharedMesh = CreateBoxMesh();
            return gameObject;
        }

        public static Material CreateMaterial(MatchController match, Color color)
        {
            Renderer sourceRenderer = match && match.Ball
                ? match.Ball.GetComponentInChildren<Renderer>()
                : null;
            if (!sourceRenderer || !sourceRenderer.sharedMaterial) return null;

            var material = new Material(sourceRenderer.sharedMaterial)
            {
                name = "Runtime Marker Material"
            };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        public static void SetMaterial(GameObject visual, Material material)
        {
            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = material;
        }

        public static void DestroyVisual(GameObject visual)
        {
            if (!visual) return;
            MeshFilter filter = visual.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh) Object.Destroy(filter.sharedMesh);
            Object.Destroy(visual);
        }

        static GameObject CreateVisual(string name)
        {
            return new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        }

        static Mesh CreateDiscMesh()
        {
            var vertices = new Vector3[DiscSegments + 1];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[DiscSegments * 3];
            normals[0] = Vector3.up;

            for (int index = 0; index < DiscSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / DiscSegments;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                normals[index + 1] = Vector3.up;

                int next = (index + 1) % DiscSegments;
                int triangle = index * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = next + 1;
                triangles[triangle + 2] = index + 1;
            }

            var mesh = new Mesh { name = "Runtime Marker Disc" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CreateBoxMesh()
        {
            Vector3[] vertices =
            {
                new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f),
                new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f),
                new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f),
                new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f)
            };
            int[] triangles =
            {
                0, 2, 1, 0, 3, 2,
                5, 6, 4, 6, 7, 4,
                4, 3, 0, 4, 7, 3,
                1, 2, 5, 2, 6, 5,
                3, 7, 2, 2, 7, 6,
                4, 0, 5, 5, 0, 1
            };

            var mesh = new Mesh { name = "Runtime Marker Box" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
