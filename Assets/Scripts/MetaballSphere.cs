using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace UnityPrototype
{
    [DisallowMultipleComponent]
    public class MetaballSphere : IMetaballShape
    {
        [SerializeField, MinValue(0.0001f)] private float m_radius = 0.5f;
        [SerializeField, Range(1, 6)] private int m_power = 1;
        [SerializeField] private Color m_color = Color.red;
        [SerializeField] private bool m_drawGizmos = true;

        public float radius => m_radius;

        public override float CalculatePotential(Vector2 targetPosition)
        {
            var d2 = (targetPosition - m_position).sqrMagnitude;
            var t = 1.0f - Mathf.Min(d2 / (m_radius * m_radius), 1.0f);
            return Mathf.Pow(t, m_power);
        }

        private void OnDrawGizmos()
        {
            if (!m_drawGizmos)
                return;

            Gizmos.color = m_color;
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}
