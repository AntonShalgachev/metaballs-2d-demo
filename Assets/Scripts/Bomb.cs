using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace UnityPrototype
{
    public class Bomb : MonoBehaviour
    {
        [SerializeField] private float m_duration = 0.1f;

        private Effector2D m_effector = null;

        private void Awake()
        {
            m_effector = GetComponent<Effector2D>();
            m_effector.enabled = false;
        }

        [Button]
        public void Explode()
        {
            StartCoroutine(ExplodeImpl());
        }

        private IEnumerator ExplodeImpl()
        {
            m_effector.enabled = true;

            var timeLeft = m_duration;
            while (timeLeft > 0.0f)
            {
                yield return new WaitForFixedUpdate();
                timeLeft -= Time.fixedDeltaTime;
            }

            m_effector.enabled = false;
        }
    }
}
