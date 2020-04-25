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

            var vertices = m_surface.GetVertices();
            var indices = new List<int>();

            for (var i = 0; i < m_surface.cellsCount; i++)
            // foreach (var cell in m_surface.EnumerateGridCells())
            {
                var configuration = m_surface.CalculateCellConfiguration(i);

                var localTriangleIndices = sm_cellTriangleIndices[configuration];

                foreach (var localIndex in localTriangleIndices)
                {
                    // var position = cell.GetCellPoint(index);
                    // vertices.Add(position);
                    // indices.Add(indices.Count);
                    indices.Add(m_surface.LocalToWorldPointIndex(i, localIndex));
                    // indices.Add(cell.LocalToWorldVertexIndex(localIndex));
                }
            }

            m_mesh.Clear();
            m_mesh.SetVertices(vertices);
            m_mesh.SetTriangles(indices, 0);
            m_mesh.RecalculateBounds();
        }
    }
}
