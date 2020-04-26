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
            private readonly MetaballSurface m_surface;

            public readonly Vector2 position;

            private int m_dataFrame;
            private bool m_dataValid => m_dataFrame == m_surface.m_frame;

            public bool debugDataValid => m_dataValid;

            private float m_value;
            public float value => m_dataValid ? m_value : 0.0f;

            private bool m_active;
            public bool active => m_dataValid ? m_active : false;

            public GridValuePoint(MetaballSurface surface, Vector2 position)
            {
                m_surface = surface;
                this.position = position;
                m_dataFrame = surface.m_frame;
                m_value = 0.0f;
                m_active = false;
            }

            public void AddValue(float valueDelta)
            {
                if (valueDelta == 0.0f)
                    return;

                if (!m_dataValid)
                {
                    m_value = 0.0f;
                    m_dataFrame = m_surface.m_frame;
                }

                m_value += valueDelta;
                m_active = m_value >= m_surface.m_isoThreshold; // TODO calculate once per frame per point
            }

            override public string ToString()
            {
                return $"GridValuePoint {{ Value={m_value} }}";
            }
        }

        public struct GridControlPoint
        {
            private MetaballSurface m_surface;

            private readonly int m_startValuePointIndex;
            private readonly int m_endValuePointIndex;
            public readonly bool valid;
            public Vector2 position => CalculatePosition();

            public GridControlPoint(MetaballSurface surface, int startValuePointIndex, int endValuePointIndex)
            {
                m_surface = surface;

                this.m_startValuePointIndex = startValuePointIndex;
                this.m_endValuePointIndex = endValuePointIndex;
                this.valid = startValuePointIndex >= 0 && endValuePointIndex >= 0;
            }

            private float CalculateRelativePosition()
            {
                if (!m_surface.m_interpolateEdgePoints)
                    return 0.5f;

                var startValue = m_surface.GetValuePointValue(m_startValuePointIndex);
                var endValue = m_surface.GetValuePointValue(m_endValuePointIndex);

                return Mathf.InverseLerp(startValue, endValue, m_surface.m_isoThreshold);
            }

            private Vector2 CalculatePosition()
            {
                if (!valid)
                    return Vector2.zero;

                var startPosition = m_surface.GetValuePointPosition(m_startValuePointIndex);
                var endPosition = m_surface.GetValuePointPosition(m_endValuePointIndex);

                return Vector2.LerpUnclamped(startPosition, endPosition, CalculateRelativePosition());
            }
        }

        public readonly struct GridCell
        {
            private readonly MetaballSurface m_surface;
            private readonly int m_index;

            private static int sm_valuePointsCount = 4;
            private readonly int[] m_valuePointsWorldIndices;

            public GridCell(MetaballSurface surface, int cellIndex)
            {
                m_surface = surface;
                m_index = cellIndex;

                m_valuePointsWorldIndices = new int[sm_valuePointsCount];
                for (var i = 0; i < m_valuePointsWorldIndices.Length; i++)
                    m_valuePointsWorldIndices[i] = m_surface.LocalToWorldValuePointIndex(m_index, i);
            }

            public int CalculateConfiguration()
            {
                int configuration = 0;
                for (var i = 0; i < sm_valuePointsCount; i++)
                {
                    var worldIndex = m_valuePointsWorldIndices[i];
                    var active = m_surface.IsValuePointActive(worldIndex);
                    var value = (1 << i) * (active ? 1 : 0);
                    configuration += value;
                }

                return configuration;
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
        private List<IMetaballShape> m_particles => m_runtimeParticles;
        private float m_particlesCount => m_particles.Count;

        private Vector2Int m_extendedGridResolution;
        public int cellsCount { get; private set; }
        private Vector2 m_inverseGridRange;
        private Vector2 m_inverseGridResolution;
        private Vector2 m_gridStep;
        private Vector2 m_gridBottomLeftPosition;

        private GridValuePoint[] m_gridValuePoints = null;
        private GridControlPoint[] m_gridControlPoints = null;
        private GridCell[] m_gridCells = null;

        private Vector3[] m_vertices;

        private HashSet<int> m_filledCells = new HashSet<int>();
        private HashSet<int> m_dirtyControlPoints = new HashSet<int>();
        public ICollection<int> filledCells => m_filledCells;

        private int m_frame = 0;

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

        private int[][] m_localToWorldValuePointIndices;
        private int[][] m_localToWorldControlPointIndices;

        private void RecreateGrid()
        {
            m_extendedGridResolution = m_gridResolution + Vector2Int.one;
            cellsCount = m_gridResolution.x * m_gridResolution.y;
            m_inverseGridRange = new Vector2(1.0f / m_gridRange.x, 1.0f / m_gridRange.y);
            m_inverseGridResolution = new Vector2(1.0f / m_gridResolution.x, 1.0f / m_gridResolution.y);
            m_gridStep = Vector2.Scale(m_gridRange, m_inverseGridResolution);
            m_gridBottomLeftPosition = 0.5f * m_gridRange - m_gridCenter;

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

            m_vertices = new Vector3[m_gridValuePoints.Length + m_gridControlPoints.Length];
            for (var i = 0; i < m_gridValuePoints.Length; i++)
                m_vertices[i] = m_gridValuePoints[i].position;

            m_localToWorldValuePointIndices = new int[cellsCount][];
            m_localToWorldControlPointIndices = new int[cellsCount][];

            for (var cellIndex = 0; cellIndex < cellsCount; cellIndex++)
            {
                m_localToWorldValuePointIndices[cellIndex] = new int[4];
                m_localToWorldControlPointIndices[cellIndex] = new int[4];

                for (var i = 0; i < 4; i++)
                {
                    m_localToWorldValuePointIndices[cellIndex][i] = CalculateLocalToWorldValuePointIndex(cellIndex, i);
                    m_localToWorldControlPointIndices[cellIndex][i] = CalculateLocalToWorldControlPointIndex(cellIndex, i);
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
            m_frame++;

            UpdateValuePoints();
        }

        private void UpdateValuePoints()
        {
            m_filledCells.Clear();

            foreach (var particle in m_particles)
            {
                particle.PrepareShape();

                var particlePos = particle.position;
                var relativePos = Vector2.Scale(m_gridBottomLeftPosition + particlePos, m_inverseGridRange);
                var location = Vector2.Scale(relativePos, m_gridResolution);

                var centerCol = Mathf.RoundToInt(location.x);
                var centerRow = Mathf.RoundToInt(location.y);

                var rangeCol = (int)(particle.radius / m_gridStep.x);
                var rangeRow = (int)(particle.radius / m_gridStep.y);

                // col: [centerCol - rangeCol; centerCol + rangeCol]
                // row: [centerRow - rangeRow; centerRow + rangeRow]

                var pointIndex = CalculateGridLinearIndex(centerCol - rangeCol, centerRow - rangeRow, m_extendedGridResolution);
                for (var dRow = -rangeRow; dRow <= rangeRow; dRow++)
                {
                    for (var dCol = -rangeCol; dCol <= rangeCol; dCol++)
                    {
                        UpdateValuePointValue(pointIndex, particle);
                        pointIndex++;
                    }
                    pointIndex += m_extendedGridResolution.x - rangeCol - rangeCol - 1;
                }

                var cellIndex = CalculateGridLinearIndex(centerCol - rangeCol - 1, centerRow - rangeRow - 1, m_gridResolution);
                for (var dRow = -rangeRow - 1; dRow <= rangeRow; dRow++)
                {
                    for (var dCol = -rangeCol - 1; dCol <= rangeCol; dCol++)
                    {
                        if (cellIndex >= 0 && cellIndex < m_gridCells.Length)
                        {
                            m_filledCells.Add(cellIndex);
                            for (var i = 0; i < 4; i++)
                                m_dirtyControlPoints.Add(LocalToWorldControlPointIndex(cellIndex, i));
                        }

                        cellIndex++;
                    }
                    cellIndex += m_gridResolution.x - rangeCol - rangeCol - 2;
                }
            }
        }

        private void UpdateValuePointValue(int pointIndex, IMetaballShape particle)
        {
            if (pointIndex < 0 || pointIndex >= m_gridValuePoints.Length)
                return;

            var value = particle.CalculatePotential(m_gridValuePoints[pointIndex].position);
            m_gridValuePoints[pointIndex].AddValue(value);
        }

        public int LocalToWorldValuePointIndex(int cellIndex, int localValuePointIndex)
        {
            return m_localToWorldValuePointIndices[cellIndex][localValuePointIndex];
        }

        public int LocalToWorldControlPointIndex(int cellIndex, int localControlPointIndex)
        {
            return m_localToWorldControlPointIndices[cellIndex][localControlPointIndex];
        }

        public int CalculateLocalToWorldValuePointIndex(int cellIndex, int localValuePointIndex)
        {
            var cols = m_gridResolution.x;
            var extendedCols = m_extendedGridResolution.x;

            var i = cellIndex % cols;
            var j = cellIndex / cols;

            var offset = sm_worldValuePointIndexOffsets[localValuePointIndex];
            return extendedCols * (j + offset.y) + (i + offset.x);
        }

        public int CalculateLocalToWorldControlPointIndex(int cellIndex, int localControlPointIndex)
        {
            var cols = m_gridResolution.x;
            var extendedCols = m_extendedGridResolution.x;

            var i = cellIndex % cols;
            var j = cellIndex / cols;

            var offset = sm_worldControlPointIndexOffsets[localControlPointIndex];
            return 2 * extendedCols * (j + offset.y) + 2 * (i + offset.x) + offset.z;
        }

        public int LocalToWorldPointIndex(int cellIndex, int localPointIndex)
        {
            var index = localPointIndex / 2;

            if (localPointIndex % 2 == 0)
                return LocalToWorldValuePointIndex(cellIndex, index);

            return LocalToWorldControlPointIndex(cellIndex, index) + m_gridValuePoints.Length;
        }

        public Vector3[] GetVertices()
        {
            foreach (var i in m_dirtyControlPoints)
                m_vertices[m_gridValuePoints.Length + i] = m_gridControlPoints[i].position;

            return m_vertices;
        }

        public Vector2 GetValuePointPosition(int index)
        {
            return m_gridValuePoints[index].position;
        }

        public float GetValuePointValue(int index)
        {
            return m_gridValuePoints[index].value;
        }

        public bool IsValuePointActive(int worldIndex)
        {
            return m_gridValuePoints[worldIndex].active;
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
                foreach (var point in m_gridControlPoints)
                {
                    if (!point.valid)
                        continue;

                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.02f);
                }

                foreach (var point in m_gridValuePoints)
                {
                    if (m_useIsoThresholdForColor)
                        Gizmos.color = point.active ? Color.white : Color.black;
                    else
                        Gizmos.color = Color.Lerp(Color.black, Color.white, point.value);

                    if (!point.debugDataValid)
                        Gizmos.color = Color.magenta;

                    Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.02f);
                }
            }
        }
    }
}
