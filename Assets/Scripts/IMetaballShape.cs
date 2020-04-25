using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPrototype
{
    public abstract class IMetaballShape : MonoBehaviour
    {
        public abstract float CalculatePotential(Vector2 position);

        protected Vector2 m_position;

        private MetaballSurface m_surface = null;

        private void Awake()
        {
            m_surface = GetComponentInParent<MetaballSurface>();
        }

        private void OnEnable()
        {
            m_surface.AddParticle(this);
        }

        private void OnDisable()
        {
            m_surface.RemoveParticle(this);
        }

        public void CachePosition()
        {
            m_position = transform.localPosition;
        }
    }
}
