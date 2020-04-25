using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPrototype
{
    public class MetaballSurfaceMesh : MonoBehaviour
    {
        [SerializeField] private MetaballSurface m_surface = null;

        private static int[][] sm_cellTriangleIndices = new int[][] {
            TriangulateConvexPolygon(new int[] {}),
            TriangulateConvexPolygon(new int[] {7, 1, 0}),
            TriangulateConvexPolygon(new int[] {3, 2, 1}),
            TriangulateConvexPolygon(new int[] {7, 3, 2, 0}),
            TriangulateConvexPolygon(new int[] {4, 3, 5}),
            TriangulateConvexPolygon(new int[] {7, 5, 4, 3, 1, 0}),
            TriangulateConvexPolygon(new int[] {5, 4, 2, 1}),
            TriangulateConvexPolygon(new int[] {7, 5, 4, 2, 0}),
            TriangulateConvexPolygon(new int[] {6, 5, 7}),
            TriangulateConvexPolygon(new int[] {6, 5, 1, 0}),
            TriangulateConvexPolygon(new int[] {7, 6, 5, 3, 2, 1}),
            TriangulateConvexPolygon(new int[] {6, 5, 3, 2, 0}),
            TriangulateConvexPolygon(new int[] {6, 4, 3, 7}),
            TriangulateConvexPolygon(new int[] {6, 4, 3, 1, 0}),
            TriangulateConvexPolygon(new int[] {7, 6, 4, 2, 1}),
            TriangulateConvexPolygon(new int[] {6, 4, 2, 0}),
        };

        private Mesh m_mesh = null;
        private List<int> m_indices = new List<int>();

        private void Awake()
        {
            m_mesh = new Mesh();
            m_mesh.MarkDynamic();

            var filter = GetComponent<MeshFilter>();
            filter.sharedMesh = m_mesh;
        }

        private static int[] TriangulateConvexPolygon(int[] vertices)
        {
            var verticesCount = vertices.Length;

            if (verticesCount < 3)
                return new int[] { };

            var trianglesCount = verticesCount - 2;

            var triangles = new int[trianglesCount * 3];

            var prevIndex = 1;
            for (var vertexIndex = 2; vertexIndex < verticesCount; vertexIndex++)
            {
                var triangleIndex = vertexIndex - 2;

                triangles[3 * triangleIndex + 0] = vertices[0];
                triangles[3 * triangleIndex + 1] = vertices[prevIndex];
                triangles[3 * triangleIndex + 2] = vertices[vertexIndex];

                prevIndex = vertexIndex;
            }

            return triangles;
        }

        private void Update()
        {
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            m_surface.UpdateField();

            CalculateMeshIndices();

            m_mesh.Clear();
            m_mesh.SetVertices(m_surface.GetVertices());
            m_mesh.SetTriangles(m_indices, 0);
            m_mesh.RecalculateBounds();
        }

        private void CalculateMeshIndices()
        {
            m_indices.Clear();

            for (var i = 0; i < m_surface.cellsCount; i++)
            {
                var configuration = m_surface.CalculateCellConfiguration(i);

                var localTriangleIndices = sm_cellTriangleIndices[configuration];

                foreach (var localIndex in localTriangleIndices)
                    m_indices.Add(m_surface.LocalToWorldPointIndex(i, localIndex));
            }
        }
    }
}
