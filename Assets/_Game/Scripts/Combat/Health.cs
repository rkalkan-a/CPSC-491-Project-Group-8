using System;
using UnityEngine;
using DungeonCrawler.Core;

namespace DungeonCrawler.Combat
{
    public class Health : MonoBehaviour
    {
        // a heart on the hud is worth this much health, so 1 damage is a quarter of a heart
        public const int HpPerHeart = 4;

        [SerializeField, Min(1)] private int maxHealth = 6;
        [SerializeField, Min(0f)] private float invulnerabilityTime = 0f; // i-frames after getting hit

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        public event Action<int, int> Changed; // current, max
        public event Action Died;

        private float invulnerableUntil = -1f;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || IsInvulnerable) return;

            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerabilityTime;

            Changed?.Invoke(Current, maxHealth);
            GameEvents.RaiseDamageTaken(this, amount);

            if (Current == 0) Die();
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        // for armor/equipment that raises max hp
        public void SetMax(int newMax, bool healToMatch = true)
        {
            int delta = newMax - maxHealth;
            maxHealth = Mathf.Max(1, newMax);
            if (healToMatch && delta > 0) Current += delta;
            Current = Mathf.Clamp(Current, 0, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        public void ResetToMax()
        {
            IsDead = false;
            Current = maxHealth;
            Changed?.Invoke(Current, maxHealth);
        }

        private void Die()
        {
            IsDead = true;
            Died?.Invoke();
            GameEvents.RaiseEntityDied(this);
        }
    }
}
