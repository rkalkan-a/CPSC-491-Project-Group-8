using System.Collections;
using UnityEngine;

namespace DungeonCrawler.Combat
{
    // flashes the sprite when Health goes down so hits are actually visible
    [RequireComponent(typeof(Health))]
    public class HurtFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField, Min(0.01f)] private float flashTime = 0.08f;
        [SerializeField, Min(1)] private int flashes = 2;

        private Health health;
        private Color originalColor;
        private int lastHp;
        private Coroutine running;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            if (target != null) originalColor = target.color;
        }

        private void OnEnable()
        {
            lastHp = health.Current;
            health.Changed += OnHealthChanged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Changed -= OnHealthChanged;
            health.Died -= OnDied;
            if (running != null) StopCoroutine(running);
            if (target != null) target.color = originalColor;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current < lastHp && target != null)
            {
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(Flash());
            }
            lastHp = current;
        }

        // stop mid flash so we don't overwrite whatever the death effect sets the color to
        private void OnDied()
        {
            if (running != null) StopCoroutine(running);
            running = null;
        }

        private IEnumerator Flash()
        {
            for (int i = 0; i < flashes; i++)
            {
                target.color = flashColor;
                yield return new WaitForSeconds(flashTime);
                target.color = originalColor;
                yield return new WaitForSeconds(flashTime);
            }
            running = null;
        }
    }
}
