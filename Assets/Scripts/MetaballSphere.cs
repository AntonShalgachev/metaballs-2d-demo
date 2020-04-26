using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace UnityPrototype
{
    [DisallowMultipleComponent]
    public class MetaballSphere : IMetaballShape
    {
        private enum Mode
        {
            Positive,
            Negative,
        }

        [SerializeField, MinValue(0.0001f)] private float m_radius = 0.5f;
        [SerializeField, Range(1, 6)] private int m_power = 1;
        [SerializeField] private Color m_color = Color.red;
        [SerializeField] private Mode m_mode = Mode.Positive;
        [SerializeField] private bool m_drawGizmos = true;

        private float m_inverseSquareRadius = 0.0f;
        private float m_squareRadius = 0.0f;
        private float m_potentialSign = 0.0f;

        public override float radius => m_radius;

        public override float CalculatePotential(Vector2 targetPosition)
        {
            var d2 = MathHelper.DistanceSqr(targetPosition, m_position);
            if (d2 > m_squareRadius)
                return 0.0f;

            var t = 1.0f - Mathf.Min(d2 * m_inverseSquareRadius, 1.0f);
            return MathHelper.IntegerPow(t, m_power) * m_potentialSign;
        }

        private void OnDrawGizmos()
        {
            if (!m_drawGizmos)
                return;

            Gizmos.color = m_color;
            GizmosHelper.DrawCircle(transform.position, m_radius);
        }

        public override void PrepareShape()
        {
            base.PrepareShape();

            m_squareRadius = m_radius * m_radius;
            m_inverseSquareRadius = 1.0f / m_squareRadius;
            m_potentialSign = m_mode == Mode.Positive ? 1.0f : -1.0f;
        }
    }
}
