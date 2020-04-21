using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPrototype
{
    public class MetaballSurfaceMesh : MonoBehaviour
    {
        [SerializeField] private MetaballSurface m_surface = null;

        private static int[][] sm_cellTriangleIndices = new int[][] {
            // TriangulateConvexPolygon(new int[] {}),
            // TriangulateConvexPolygon(new int[] {0, 1, 7}),
            // TriangulateConvexPolygon(new int[] {1, 2, 3}),
            // TriangulateConvexPolygon(new int[] {0, 2, 3, 7}),
            // TriangulateConvexPolygon(new int[] {5, 3, 4}),
            // TriangulateConvexPolygon(new int[] {0, 1, 3, 4, 5, 7}),
            // TriangulateConvexPolygon(new int[] {1, 2, 4, 5}),
            // TriangulateConvexPolygon(new int[] {0, 2, 4, 5, 7}),
            // TriangulateConvexPolygon(new int[] {7, 5, 6}),
            // TriangulateConvexPolygon(new int[] {0, 1, 5, 6}),
            // TriangulateConvexPolygon(new int[] {1, 2, 3, 5, 6, 7}),
            // TriangulateConvexPolygon(new int[] {0, 2, 3, 5, 6}),
            // TriangulateConvexPolygon(new int[] {7, 3, 4, 6}),
            // TriangulateConvexPolygon(new int[] {0, 1, 3, 4, 6}),
            // TriangulateConvexPolygon(new int[] {1, 2, 4, 6, 7}),
            // TriangulateConvexPolygon(new int[] {0, 2, 4, 6}),

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

        private static int[][] sm_cellEdgePoints = new int[][] {
            new int[] { 1, 3 },
            new int[] { 1, 5 },
            new int[] { 1, 7 },
            new int[] { 3, 5 },
            new int[] { 3, 7 },
            new int[] { 5, 7 },
        };

        private static int[][] sm_cellEdges = new int[][] {
            new int[] {},
            new int[] {2},
            new int[] {0},
            new int[] {4},
            new int[] {3},
            new int[] {0, 5},
            new int[] {1},
            new int[] {5},
            new int[] {5},
            new int[] {1},
            new int[] {2, 3},
            new int[] {3},
            new int[] {4},
            new int[] {0},
            new int[] {2},
            new int[] {},
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

            var vertices = new List<Vector3>();
            var indices = new List<int>();

            foreach (var cell in m_surface.EnumerateGridCells())
            {
                var configuration = cell.CalculateConfiguration();

                var localTriangleIndices = sm_cellTriangleIndices[configuration];

                foreach (var index in localTriangleIndices)
                {
                    var position = cell.GetCellPoint(index);
                    vertices.Add(position);
                    indices.Add(indices.Count);
                }

                foreach (var edge in sm_cellEdges[configuration])
                {
                    var edgePoints = sm_cellEdgePoints[edge];

                    var from = cell.GetCellPoint(edgePoints[0]);
                    var to = cell.GetCellPoint(edgePoints[1]);

                    Debug.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to), Color.red);
                }
            }

            m_mesh.Clear();
            m_mesh.SetVertices(vertices);
            m_mesh.SetTriangles(indices, 0);
            // m_mesh.RecalculateNormals();
            m_mesh.RecalculateBounds();
        }
    }
}
