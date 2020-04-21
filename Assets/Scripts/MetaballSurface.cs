using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace UnityPrototype
{
    public class MetaballSurface : MonoBehaviour
    {
        public class GridPoint
        {
            private MetaballSurface m_surface = null;

            public Vector2 position = Vector2.zero;
            public float value = 0.0f;

            public bool active => value >= m_surface.m_isoThreshold;

            public GridPoint(MetaballSurface surface, Vector2 position)
            {
                m_surface = surface;

                this.position = position;
            }

            override public string ToString()
            {
                return $"GridPoint {{ Value={value} }}";
            }
        }

        public class GridCell
        {
            private MetaballSurface m_surface = null;

            public List<GridPoint> vertices;

            public GridCell(MetaballSurface surface, List<GridPoint> vertices)
            {
                m_surface = surface;

                this.vertices = vertices;
            }

            public int CalculateConfiguration()
            {
                int configuration = 0;
                for (var i = 0; i < vertices.Count; i++)
                {
                    var active = vertices[i].active;
                    var value = (1 << i) * (active ? 1 : 0);
                    configuration += value;
                }

                return configuration;
            }

            public Vector2 GetEdgePoint(int edgePointId)
            {
                var sourceVertex = vertices[edgePointId];
                var destinationVertex = vertices[(edgePointId + 1) % vertices.Count];

                var relativePosition = 0.5f;
                if (m_surface.m_interpolateEdgePoints)
                    relativePosition = Mathf.InverseLerp(sourceVertex.value, destinationVertex.value, m_surface.m_isoThreshold);

                return Vector2.Lerp(sourceVertex.position, destinationVertex.position, relativePosition);
            }

            public Vector2 GetCellPoint(int pointId)
            {
                var index = pointId / 2;

                if (pointId % 2 == 1)
                    return GetEdgePoint(index);

                return vertices[index].position;
            }
        }

        [SerializeField] private Transform m_gridTransform = null;
        [SerializeField] private Vector2Int m_gridResolution = Vector2Int.one * 10;
        [SerializeField, MinValue(0.00001f)] private float m_isoThreshold = 0.5f;
        [SerializeField] private bool m_interpolateEdgePoints = true;

        [Header("Gizmos")]
        [SerializeField] private bool m_drawPointGizmos = false;
        [SerializeField] private bool m_useIsoThresholdForColor = true;

        [ShowNativeProperty] private Vector2 m_gridRange => m_gridTransform != null ? (Vector2)m_gridTransform.localScale : Vector2.one;
        [ShowNativeProperty] private Vector2 m_gridCenter => m_gridTransform != null ? (Vector2)m_gridTransform.position : Vector2.one;

        private List<IMetaballShape> m_runtimeParticles = new List<IMetaballShape>();
        private List<IMetaballShape> m_particles
        {
            get
            {
                if (!Application.isPlaying)
                    return FindParticles();

                return m_runtimeParticles;
            }
        }
        private float m_particlesCount => m_particles.Count;

        private GridPoint[,] m_gridPoints = null;
        private GridCell[,] m_gridCells = null;

        private List<IMetaballShape> FindParticles()
        {
            var particles = GetComponentsInChildren<IMetaballShape>(true);
            return new List<IMetaballShape>(particles);
        }

        public void AddParticle(IMetaballShape particle)
        {
            m_runtimeParticles.Add(particle);
        }

        public void RemoveParticle(IMetaballShape particle)
        {
            m_runtimeParticles.Remove(particle);
        }

        private void Awake()
        {
            RecreateGrid();
        }

        private GridPoint CreateGridPoint(Vector2Int location)
        {
            var inverseResolution = new Vector2(1.0f / m_gridResolution.x, 1.0f / m_gridResolution.y);
            var relativePos = Vector2.Scale(location, inverseResolution); // [0; 1]
            var position = m_gridCenter - 0.5f * m_gridRange + Vector2.Scale(relativePos, m_gridRange);

            return new GridPoint(this, position);
        }

        private GridPoint GetGridPoint(Vector2Int location)
        {
            return m_gridPoints[location.x, location.y];
        }

        private GridCell CreateGridCell(Vector2Int location)
        {
            // TODO create only once
            var pointOffsets = new List<Vector2Int>
            {
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.one,
                Vector2Int.up,
            };

            var cellVertices = new List<GridPoint>();
            foreach (var offset in pointOffsets)
                cellVertices.Add(GetGridPoint(location + offset));

            return new GridCell(this, cellVertices);
        }

        private GridCell GetGridCell(Vector2Int location)
        {
            return m_gridCells[location.x, location.y];
        }

        public IEnumerable<GridPoint> EnumerateGridPoints()
        {
            for (var i = 0; i <= m_gridResolution.x; i++)
                for (var j = 0; j <= m_gridResolution.y; j++)
                    yield return m_gridPoints[i, j];
        }

        public IEnumerable<GridCell> EnumerateGridCells()
        {
            for (var i = 0; i < m_gridResolution.x; i++)
                for (var j = 0; j < m_gridResolution.y; j++)
                    yield return m_gridCells[i, j];
        }

        private void RecreateGrid()
        {
            m_gridPoints = new GridPoint[m_gridResolution.x + 1, m_gridResolution.y + 1];
            for (var i = 0; i <= m_gridResolution.x; i++)
                for (var j = 0; j <= m_gridResolution.y; j++)
                    m_gridPoints[i, j] = CreateGridPoint(new Vector2Int(i, j));

            m_gridCells = new GridCell[m_gridResolution.x, m_gridResolution.y];
            for (var i = 0; i < m_gridResolution.x; i++)
                for (var j = 0; j < m_gridResolution.y; j++)
                    m_gridCells[i, j] = CreateGridCell(new Vector2Int(i, j));
        }

        public void UpdateField()
        {
            foreach (var point in EnumerateGridPoints())
            {
                point.value = 0.0f;
                foreach (var particle in m_particles)
                    point.value += particle.CalculatePotential(point.position);
            }

            // foreach (var cell in EnumerateGridCells())
            // {
            //     var configuration = cell.CalculateConfiguration();

            //     var indices = sm_cellTriangleIndices[configuration];



            //     // foreach (var edge in sm_cellEdges[configuration])
            //     // {
            //     //     var edgePoints = sm_cellEdgePoints[edge];

            //     //     var from = cell.GetCellPoint(edgePoints[0]);
            //     //     var to = cell.GetCellPoint(edgePoints[1]);

            //     //     Debug.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to), Color.red);
            //     // }
            // }
        }

        private void OnDrawGizmos()
        {
            if (m_gridTransform != null)
            {
                var gridCenter = m_gridCenter;
                var gridRange = m_gridRange;

                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(gridCenter, gridRange);

                Gizmos.color = Color.red;
                GizmosHelper.DrawVector(gridCenter, Vector3.right, 0.1f);
                Gizmos.color = Color.green;
                GizmosHelper.DrawVector(gridCenter, Vector3.up, 0.1f);
                Gizmos.color = Color.blue;
                GizmosHelper.DrawVector(gridCenter, Vector3.forward, 0.1f);
            }

            if (m_drawPointGizmos && m_gridPoints != null)
            {
                foreach (var point in m_gridPoints)
                {
                    if (m_useIsoThresholdForColor)
                        Gizmos.color = point.active ? Color.white : Color.black;
                    else
                        Gizmos.color = Color.Lerp(Color.black, Color.white, point.value);

                    Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.02f);
                }
            }
        }
    }
}
