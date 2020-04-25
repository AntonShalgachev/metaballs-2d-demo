using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace UnityPrototype
{
    public class MetaballSurface : MonoBehaviour
    {
        // Grid cell:
        // 6        5        4
        //   X------o------X
        //   |             |
        //   |             |
        // 7 o             o 3
        //   |             |
        //   |             |
        //   X------o------X
        // 0        1        2
        // 
        // X -- Grid value point
        // o -- Grid control point
        // i -- cell point local index

        public struct GridValuePoint
        {
            private MetaballSurface m_surface;

            public Vector2 position;

            private float m_value;
            public float value
            {
                get => m_value;
                set
                {
                    m_value = value;
                    m_active = m_value >= m_surface.m_isoThreshold;
                }
            }

            private bool m_active;
            public bool active => m_active;

            public GridValuePoint(MetaballSurface surface, Vector2 position)
            {
                m_surface = surface;
                this.position = position;
                m_value = 0.0f;
                m_active = false;
            }

            override public string ToString()
            {
                return $"GridValuePoint {{ Value={value} }}";
            }
        }

        public struct GridControlPoint
        {
            private MetaballSurface m_surface;

            public readonly int startValuePointIndex;
            public readonly int endValuePointIndex;
            public readonly bool valid;
            public float relativePosition;
            public Vector2 position => CalculatePosition();

            public GridControlPoint(MetaballSurface surface, int startValuePointIndex, int endValuePointIndex)
            {
                m_surface = surface;

                this.startValuePointIndex = startValuePointIndex;
                this.endValuePointIndex = endValuePointIndex;
                this.valid = startValuePointIndex >= 0 && endValuePointIndex >= 0;

                relativePosition = 0.5f;
            }

            private Vector2 CalculatePosition()
            {
                var startPosition = m_surface.GetValuePointPosition(startValuePointIndex);
                var endPosition = m_surface.GetValuePointPosition(endValuePointIndex);

                return Vector2.LerpUnclamped(startPosition, endPosition, relativePosition);
            }
        }

        public class GridCell
        {
            private MetaballSurface m_surface = null;
            private int m_index = 0;

            private static int sm_valuePointsCount = 4;

            public GridCell(MetaballSurface surface, int cellIndex)
            {
                m_surface = surface;
                m_index = cellIndex;
            }

            public int CalculateConfiguration()
            {
                int configuration = 0;
                for (var i = 0; i < sm_valuePointsCount; i++)
                {
                    var worldValuePointIndex = m_surface.LocalToWorldValuePointIndex(m_index, i);
                    var active = m_surface.IsValuePointActive(worldValuePointIndex);
                    var value = (1 << i) * (active ? 1 : 0);
                    configuration += value;
                }

                return configuration;
            }

            // public Vector2 GetEdgePoint(int edgePointId)
            // {
            //     var sourceVertex = vertices[edgePointId];
            //     var destinationVertex = vertices[(edgePointId + 1) % vertices.Count];

            //     var relativePosition = 0.5f;
            //     if (m_surface.m_interpolateEdgePoints)
            //         relativePosition = Mathf.InverseLerp(sourceVertex.value, destinationVertex.value, m_surface.m_isoThreshold);

            //     return Vector2.Lerp(sourceVertex.position, destinationVertex.position, relativePosition);
            // }

            // public Vector2 GetCellPoint(int pointId)
            // {
            //     var index = pointId / 2;

            //     if (pointId % 2 == 1)
            //         return GetEdgePoint(index);

            //     return vertices[index].position;
            // }

            // public int LocalToWorldVertexIndex(int localIndex)
            // {
            //     return m_surface.LocalToWorldPointIndex(m_index, localIndex);
            // }
        }

        // private enum GridPointType
        // {
        //     Value,
        //     Control,
        // }

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

        private Vector2Int m_extendedGridResolution;
        public int cellsCount;

        private GridValuePoint[] m_gridValuePoints = null;
        private GridControlPoint[] m_gridControlPoints = null;
        private GridCell[] m_gridCells = null;

        private static readonly Vector2Int[] sm_worldValuePointIndexOffsets = new Vector2Int[] {
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.one,
                Vector2Int.up,
            };

        private static readonly Vector3Int[] sm_worldControlPointIndexOffsets = new Vector3Int[] {
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 1),
                new Vector3Int(0, 0, 0),
            };

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
            m_extendedGridResolution = m_gridResolution + Vector2Int.one;
            cellsCount = m_gridResolution.x * m_gridResolution.y;
            RecreateGrid();
        }

        private static int CalculateGridLinearIndex(int x, int y, Vector2Int gridResolution)
        {
            return x + y * gridResolution.x;
        }

        private static Vector2 GridLocationToPosition(Vector2 inverseResolution, Vector2 center, Vector2 range, Vector2Int location)
        {
            var relativePos = Vector2.Scale(location, inverseResolution); // [0; 1]
            return center - 0.5f * range + Vector2.Scale(relativePos, range);
        }

        private GridValuePoint CreateGridValuePoint(Vector2Int location)
        {
            var inverseResolution = new Vector2(1.0f / m_gridResolution.x, 1.0f / m_gridResolution.y); // TODO calculate once
            var position = GridLocationToPosition(inverseResolution, m_gridCenter, m_gridRange, location);

            return new GridValuePoint(this, position);
        }

        private GridControlPoint CreateGridControlPoint(Vector2Int cellLocation, Vector2Int endLocationOffset)
        {
            // var inverseResolution = new Vector2(1.0f / m_gridResolution.x, 1.0f / m_gridResolution.y); // TODO calculate once
            // var position = GridLocationToPosition(inverseResolution, m_gridCenter, m_gridRange, location);

            var startLocation = cellLocation;
            var endLocation = cellLocation + endLocationOffset;
            var startIndex = CalculateGridLinearIndex(startLocation.x, startLocation.y, m_extendedGridResolution);
            var endIndex = CalculateGridLinearIndex(endLocation.x, endLocation.y, m_extendedGridResolution);

            if (startIndex >= m_gridValuePoints.Length)
                startIndex = -1;
            if (endIndex >= m_gridValuePoints.Length)
                endIndex = -1;

            return new GridControlPoint(this, startIndex, endIndex);
        }

        private GridCell GetGridCell(Vector2Int location)
        {
            return m_gridCells[CalculateGridLinearIndex(location.x, location.y, m_gridResolution)];
        }

        private void RecreateGrid()
        {
            // TODO refactor this fucking index mess

            m_gridValuePoints = new GridValuePoint[m_extendedGridResolution.x * m_extendedGridResolution.y];
            m_gridControlPoints = new GridControlPoint[2 * m_extendedGridResolution.x * m_extendedGridResolution.y];
            for (var i = 0; i < m_extendedGridResolution.x; i++)
            {
                for (var j = 0; j < m_extendedGridResolution.y; j++)
                {
                    var baseIndex = CalculateGridLinearIndex(i, j, m_extendedGridResolution);
                    var location = new Vector2Int(i, j);
                    m_gridValuePoints[baseIndex] = CreateGridValuePoint(location);
                    m_gridControlPoints[2 * baseIndex + 0] = CreateGridControlPoint(location, Vector2Int.up);
                    m_gridControlPoints[2 * baseIndex + 1] = CreateGridControlPoint(location, Vector2Int.right);
                }
            }

            m_gridCells = new GridCell[m_gridResolution.x * m_gridResolution.y];
            for (var i = 0; i < m_gridResolution.x; i++)
            {
                for (var j = 0; j < m_gridResolution.y; j++)
                {
                    var cellIndex = CalculateGridLinearIndex(i, j, m_gridResolution);
                    m_gridCells[cellIndex] = new GridCell(this, cellIndex);
                }
            }
        }

        public void UpdateField()
        {
            foreach (var particle in m_particles)
                particle.CachePosition();

            for (var i = 0; i < m_gridValuePoints.Length; i++)
            {
                m_gridValuePoints[i].value = 0.0f;
                foreach (var particle in m_particles)
                    m_gridValuePoints[i].value += particle.CalculatePotential(m_gridValuePoints[i].position);
            }

            for (var i = 0; i < m_gridControlPoints.Length; i++)
            {
                if (!m_gridControlPoints[i].valid)
                    continue;

                var startValuePointIndex = m_gridControlPoints[i].startValuePointIndex;
                var endValuePointIndex = m_gridControlPoints[i].endValuePointIndex;

                var relativePosition = 0.5f;
                if (m_interpolateEdgePoints)
                    relativePosition = Mathf.InverseLerp(m_gridValuePoints[startValuePointIndex].value, m_gridValuePoints[endValuePointIndex].value, m_isoThreshold);
                m_gridControlPoints[i].relativePosition = relativePosition;
            }
        }

        // public int LocalToWorldPointIndex(int cellIndex, int localPointIndex)
        // {
        //     return 0;
        // }

        public int LocalToWorldValuePointIndex(int cellIndex, int localValuePointIndex)
        {
            var cols = m_gridResolution.x;
            var extendedCols = m_extendedGridResolution.x;

            var i = cellIndex % cols;
            var j = cellIndex / cols;

            var offset = sm_worldValuePointIndexOffsets[localValuePointIndex];
            return extendedCols * (j + offset.y) + (i + offset.x);

            // // TODO don't use dynamic arrays
            // var worldIndices = new int[] {
            //     extendedCols * (j + 0) + (i + 0),
            //     extendedCols * (j + 0) + (i + 1),
            //     extendedCols * (j + 1) + (i + 1),
            //     extendedCols * (j + 1) + (i + 0),
            // };

            // return worldIndices[localValuePointIndex];
        }

        public int LocalToWorldControlPointIndex(int cellIndex, int localControlPointIndex)
        {
            var cols = m_gridResolution.x;
            var extendedCols = m_extendedGridResolution.x;

            var i = cellIndex % cols;
            var j = cellIndex / cols;

            var offset = sm_worldControlPointIndexOffsets[localControlPointIndex];
            return 2 * extendedCols * (j + offset.y) + 2 * (i + offset.x) + offset.z;

            // // TODO don't use dynamic arrays
            // var worldIndices = new int[] {
            //     2 * extendedCols * (j + 0) + 2 * (i + 0) + 1,
            //     2 * extendedCols * (j + 0) + 2 * (i + 1) + 0,
            //     2 * extendedCols * (j + 1) + 2 * (i + 0) + 1,
            //     2 * extendedCols * (j + 0) + 2 * (i + 0) + 0,
            // };

            // return worldIndices[localControlPointIndex];
        }

        public int LocalToWorldPointIndex(int cellIndex, int localPointIndex)
        {
            var index = localPointIndex / 2;

            if (localPointIndex % 2 == 0)
                return LocalToWorldValuePointIndex(cellIndex, index);

            return LocalToWorldControlPointIndex(cellIndex, index) + m_gridValuePoints.Length;
        }

        public List<Vector3> GetVertices()
        {
            var vertices = new List<Vector3>();

            foreach (var point in m_gridValuePoints)
                vertices.Add(point.position);
            foreach (var point in m_gridControlPoints)
                vertices.Add(point.valid ? point.position : Vector2.zero);

            return vertices;
        }

        public Vector2 GetValuePointPosition(int index)
        {
            return m_gridValuePoints[index].position;
        }

        public bool IsValuePointActive(int index)
        {
            return m_gridValuePoints[index].active;
        }

        public int CalculateCellConfiguration(int cellIndex)
        {
            return m_gridCells[cellIndex].CalculateConfiguration();
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

            if (m_drawPointGizmos && m_gridValuePoints != null)
            {
                foreach (var point in m_gridValuePoints)
                {
                    if (m_useIsoThresholdForColor)
                        Gizmos.color = point.active ? Color.white : Color.black;
                    else
                        Gizmos.color = Color.Lerp(Color.black, Color.white, point.value);

                    Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.02f);
                }

                foreach (var point in m_gridControlPoints)
                {
                    if (!point.valid)
                        continue;

                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.02f);
                }
            }
        }
    }
}
