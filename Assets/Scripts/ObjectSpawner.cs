using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace UnityPrototype
{
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject m_particlePrefab = null;
        [SerializeField] private Transform m_particlesRoot = null;
        [SerializeField] private float m_interval = 0.1f;
        [SerializeField] private int m_batchSize = 3;
        [SerializeField] private int m_maxParticles = -1;
        [SerializeField] private float m_spread = 0.0f;

        private float m_delay = 0.0f;
        [ShowNonSerializedField] private int m_spawnedParticles = 0;

        private void FixedUpdate()
        {
            m_delay -= Time.fixedDeltaTime;

            if (m_maxParticles > 0 && m_spawnedParticles >= m_maxParticles)
                return;

            if (m_delay < 0.0f)
            {
                Spawn();
                m_delay = m_interval;
            }
        }

        private void Spawn()
        {
            for (var i = 0; i < m_batchSize; i++)
            {
                var offset = Random.insideUnitCircle * m_spread;
                Instantiate(m_particlePrefab, transform.position + (Vector3)offset, Quaternion.identity, m_particlesRoot);
                m_spawnedParticles++;
            }
        }
    }
}
